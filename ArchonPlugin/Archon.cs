using Assets.Behavior.Components.Motion;
using AVS;
using AVS.Assets;
using AVS.Composition;
using AVS.Configuration;
using AVS.Interfaces;
using AVS.Log;
using AVS.SaveLoad;
using AVS.Util;
using AVS.VehicleComponents;
using AVS.VehicleParts;
using AVS.VehicleTypes;
using FMOD.Studio;
using FMODUnity;
using Subnautica_Archon.Components;
using Subnautica_Archon.Modules;
using Subnautica_Archon.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Logger = AVS.Logger;

namespace Subnautica_Archon
{



    public class Archon : Submarine, IPowerListener, IProtoTreeEventListener, IAutopilotEventListener
    {
        public static GameObject? staticModel;
        private ArchonControl? control;
        public ArchonControl Control => control.OrThrow(
            () =>
            new InvalidOperationException($"Trying to access Control before Awake()"));

        public LogWriter Log { get; }

        public static readonly VehicleColor defaultBaseColor = new VehicleColor(new Color(0xDE, 0xDE, 0xDE) / 255f);
        public static readonly VehicleColor defaultStripeColor = new VehicleColor(new Color(0x3F, 0x4C, 0x7A) / 255f);

        //private List<GameObject> tetherSources;
        //tracks true if vehicle death was ever determined. Can't enter in this state
        private bool wasDead;
        /// <summary>
        /// True if this component has been destroyed and is no longer usable.
        /// </summary>
        public bool destroyed;
        private float deathAge;
        //private MyLogger Log { get; }
        private MassDrive? engine;
        private EnergyInterface? energyInterface;
        private int[] moduleCounts = new int[Enum.GetValues(typeof(ArchonModule)).Length];

        private bool clippingWater;
        private bool isInCriticalRecovery = false;
        private Dictionary<string, VoiceLibrary> VoiceLibraries { get; } = new Dictionary<string, VoiceLibrary>();

        public Archon() : base(new VehicleConfiguration(
            unlockedSprite: MainPatcher.StaticImages.ArchonCraftingSprite.Sprite,
            maxHealth: 20000,
            crushDamage: 20000 / (60 * 2),    //damage so that total failure is achieved after 2 minutes at crush depth
            mass: 20000,
            numModules: 8,
            craftingSprite: MainPatcher.StaticImages.ArchonCraftingSprite.AtlasSprite,
            pingSprite: MainPatcher.StaticImages.ArchonPingSprite.AtlasSprite,
            saveFileSprite: MainPatcher.StaticImages.ArchonPingSprite.Sprite,
            moduleBackgroundImage: MainPatcher.StaticImages.ArchonModuleBackground.Sprite,
            description: Language.main.Get("General.Description"),
            encyclopediaEntry: Language.main.Get("General.Encyclopedia"),
            canLeviathanGrab: false,
            canMoonpoolDock: false,
            pilotingStyle: PilotingStyle.Other,
            materialAdaptConfig: new MaterialAdaptConfig(),
            recipe: NewRecipe
                .StartWith(TechType.PowerCell, 1)
                .Include(TechType.AdvancedWiringKit, 2)
                .Include(TechType.Diamond, 2)
                .Include(TechType.PlasteelIngot, 4)
                .Done(),
            getVoiceSoundVolume: () => MainPatcher.PluginConfig.voiceVolumePercent / 100f
            / 5 //the archon uses a shitload of tethers and each is a player in VF/AVS
            ,
            getVoiceSubtitlesEnabled: () => MainPatcher.PluginConfig.showVoiceSubtitles
        ))
        {
            Log = new LogWriter(
                prefix: $"V" + Id,
                "Mod");
            //Log = new MyLogger(this);
            Log.Write($"Constructed");
            MenuTracker.OnOpen += () =>
            {
                if (control != null)
                    control.PrepareForSaving();
            };
            //MaterialFixer = new MaterialFixer(this, Logging.Verbose);
        }


        protected override void CreateDataBlocks(Action<DataBlock> addBlock)
        {
            addBlock(new DataBlock(
                "Archon",
                    Persistable.Property("IsInCriticalRecovery",
                        () => isInCriticalRecovery,
                        b => isInCriticalRecovery = b
                        ),
                    Persistable.Property("Docked",
                        () => Control.bayControl.Docked
                            .Select(x => x.GameObject.PrefabId())
                            .Where(x => x != null)
                            .Select(x => x!.Id)
                            .ToList(),
                        list =>
                        {
                            DockedSubPrefabIds = list;
                            Log.Write($"Docked sub prefabs restored from file: {string.Join(", ", list)}");
                        }
                        )
                ));
            base.CreateDataBlocks(addBlock);
        }

        //public override float ExitVelocityLimit => 100f;    //any speed is good
        public override bool LogDebug => true;
        public IEnumerable<QuickSlot> QuickSlots
        {
            get
            {
                for (int i = 0; i < slotIDs.Length; i++)
                    yield return new QuickSlot(i, slotIDs[i]);
            }
        }

        public override void OnFinishedLoading()
        {
            base.OnFinishedLoading();
            Log.Write($"Comparing colors {BaseColor} and {StripeColor}");
            if (BaseColor == VehicleColor.Default && StripeColor == VehicleColor.Default)
            {
                Log.Write($"Resetting default color {VehicleName}");
                SetBaseColor(defaultBaseColor);
                SetStripeColor(defaultStripeColor);
            }

            Control.RedetectDocked();
        }

        //public static Sprite? saveFileSprite, moduleBackground;
        //public static Atlas.Sprite? craftingSprite, pingSprite;
        //public static Atlas.Sprite emptySprite = new Atlas.Sprite(Texture2D.blackTexture);
        //public override Atlas.Sprite CraftingSprite => craftingSprite ?? base.CraftingSprite;
        //public override Atlas.Sprite PingSprite => pingSprite ?? base.PingSprite;
        //public override Sprite SaveFileSprite => saveFileSprite ?? base.SaveFileSprite;
        //public override Sprite ModuleBackgroundImage => moduleBackground ?? base.ModuleBackgroundImage;
        //public override string Description => Language.main.Get("description");
        //public override string EncyclopediaEntry => Language.main.Get("encyclopedia");

        //public override Dictionary<TechType, int> Recipe =>
        //    new Dictionary<TechType, int> {
        //        { TechType.PowerCell, 1 },
        //        { TechType.AdvancedWiringKit, 2 },
        //        //{ TechType.UraniniteCrystal, 3 },
        //        //{ TechType.Lead, 3 },
        //        { TechType.Diamond, 2 },
        //        //{ TechType.Kyanite, 2 },
        //        { TechType.PlasteelIngot, 4 },
        //    };


        public static GameObject GetAssets()
        {
            try
            {
                Util.Log.Write(nameof(GetAssets));
                var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string bundlePath;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    bundlePath = Path.Combine(modPath, "archon.osx");
                else
                    bundlePath = Path.Combine(modPath, "archon");
                Util.Log.Write($"Trying to load asset bundle from '{bundlePath}'");
                if (!File.Exists(bundlePath))
                    Util.Log.Write("This file does not appear to exist");
                var bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle != null)
                {
                    var assets = bundle.LoadAllAssets();
                    foreach (var obj in assets)
                    {
                        Util.Log.Write("Scanning object: " + obj.name);
                        if (obj.name == "Archon")
                        {
                            staticModel = (GameObject)obj;
                        }
                    }
                    if (staticModel == null)
                        Util.Log.Write("Model not found among: " + string.Join(", ", Helper.Names(assets)));
                }
                else
                    Util.Log.Write("Unable to loade bundle from path");
                Util.Log.Write(nameof(GetAssets) + " done");
            }
            catch (Exception ex)
            {
                Util.Log.Write(nameof(GetAssets), ex);
            }
            return staticModel.OrThrow(() => throw new IOException("Unable to load Archon model. Please check your installation"));
        }

        void OnDestroy()
        {
            Util.Log.Write($"{VehicleName} " + nameof(OnDestroy));
            destroyed = true;
        }


        private bool isInitialized = false;
        private bool hadUnpausedFrame = false;

        public override void SubConstructionComplete()
        {
            base.SubConstructionComplete();
            SetBaseColor(defaultBaseColor);
            SetStripeColor(defaultStripeColor);
        }

        public override void Awake()
        {
            Util.Log.Write(nameof(Awake));
            worldForces.aboveWaterDrag = worldForces.underwaterDrag = 0;


            BayControl.OnDockingFailedFull = (archon, d) =>
            {
                Log.Write($"full");
                AVS.Logger.PDANote("Cannot dock: Hangar is full", 3f);
            };

            BayControl.OnDockingFailedTooLarge = (archon, d) =>
            {
                Log.Write($"too large");
                AVS.Logger.PDANote("Cannot dock: Your vehicle is too large", 3f);
            };

            //onToggle += OnQuickbarToggle;



            control = GetComponent<ArchonControl>();
            control.freeCamera = MainPatcher.PluginConfig.defaultToFreeCamera;
            control.interiorLightScale = 0.75f;

            //var loadSave = gameObject.GetComponent<LoadSaveComponent>();
            //if (!loadSave)
            //    loadSave = gameObject.AddComponent<LoadSaveComponent>();
            //loadSave.control = control;

            Destroy(modulesRoot);

            modulesRoot = control.hangarRoot.gameObject.AddComponent<ChildObjectIdentifier>();

            var interior = transform.Find("Interior");
            if (interior)
            {
                var reactorTransform = interior.Find("Bioreactor");
                if (reactorTransform)
                {
                    var reactor = reactorTransform.gameObject.EnsureComponent<MaterialReactor>();
                    reactor.Initialize(this, 6, 6, AVS.Localization.Text.Translated("Component.ArchonBioreactor"), 0, MaterialReactor.GetBioReactorData());
                    reactor.canViewWhitelist = false;
                    reactor.localizeInteractText = true;
                }
                else
                    Log.Error("Unable to find Biofuel Storage child");
            }
            else
                Log.Error("Unable to find Interior child");



            var mapWorld = transform.Find("Interior/Map Table/Display/World");
            if (mapWorld != null)
            {
                Log.Write($"Found map world {mapWorld.NiceName()}. Trying to build mini-world");
                try
                {
                    SpawnMiniWorld(mapWorld, Control.mapHologramMaterial, 500);
                    Log.Write($"Map instantiated");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error instantiating map", ex);
                }
            }
            else
            {
                Log.Write($"Water tank not found");
            }


            base.Awake();

            //Log.Write("Checking quickslots");
            //foreach (var s in QuickSlots)
            //{
            //    var mod = modules.GetItemInSlot(s.ID);
            //    if (mod != null && mod.item == null)
            //    {
            //        Log.Error($"Found invalid item in slot {s}. Purging");
            //        modules.RemoveItem(s.ID, true, false);
            //    }
            //}


            //var cameraController = gameObject.GetComponentInChildren<AVS.VehicleComponents.MVCameraController>();
            //if (cameraController)
            //{
            //    Log.Write($"Destroying camera controller {cameraController}");
            //    Destroy(cameraController);
            //}


        }

        //private void OnQuickbarToggle(int slotID, bool state)
        //{
        //    if (state == true)
        //    {
        //        var slotId = new QuickSlot(slotID, slotIDs[slotID]);
        //        var item = modules.GetItemInSlot(slotId.ID)?.item;
        //        if (item == null)
        //            Log.Error($"No item found in slot {slotID}/{slotId}");
        //        else
        //        {
        //            var vehicle = item.gameObject.GetComponent<Vehicle>();
        //            if (!vehicle)
        //                Log.Error($"Item found in slot {slotID}/{slotId} ({item.gameObject}) is not a vehicle");
        //            else
        //            {
        //                var cr = Control.CheckUndocking(vehicle.gameObject);
        //                if (cr == UndockingCheckResult.Ok)
        //                {
        //                    AbortAutoLeveling();
        //                    Log.Write($"Removing quick bar item in slot [{slotId}]");
        //                    var removed = modules.RemoveItem(slotId.ID, true, true);
        //                    Log.Write($"Removed [{removed}]");


        //                    Log.Write($"Undocking {Util.Log.Describe(vehicle)}");
        //                    Control.Undock(vehicle.gameObject);
        //                    ToggleSlot(slotID, false);
        //                    if (Drone.IsOne(vehicle))
        //                        SignalQuickslotsChangedWhilePiloting(slotId);
        //                }
        //                else
        //                {
        //                    ToggleSlot(slotID, false);
        //                    ErrorMessage.AddError($"Cannot undock right now ({cr})");
        //                }
        //            }
        //        }
        //    }

        //}


        private Coroutine? autoLevelRoutine;
        public override void DeselectSlots()
        {
            Log.Write(nameof(DeselectSlots));
            if (exitLimitsSuspended)
                base.DeselectSlots();
            else
            {
                if (!AbortAutoLeveling())
                {
                    Log.Write("Starting new exit loop");
                    autoLevelRoutine = StartCoroutine(AutoLevelThenExit());
                }
            }
        }

        public bool AbortAutoLeveling()
        {
            if (autoLevelRoutine != null)
            {
                Log.Write("Exit loop in progress. Aborting");
                StopCoroutine(autoLevelRoutine);
                autoLevelRoutine = null;
                Logger.PDANote($"Auto-leveling aborted");
                Control.doAutoLevel = false;
                Log.Write("Aborted. Control restored");
                return true;
            }
            return false;
        }

        private IEnumerator AutoLevelThenExit()
        {
            if (Control.IsLevel)
            {
                Log.Write("Archon is level. Exiting now");
                base.DeselectSlots();
                autoLevelRoutine = null;
                yield break;
            }

            Log.Write("Archon is not level. Leveling out");
            Control.doAutoLevel = true;
            Logger.PDANote($"Leveling out. Please stand by");
            //var timewindow = TimeSpan.FromSeconds(5);
            //var deadline = DateTime.Now + timewindow;
            float timewindow = 5;
            var remaining = timewindow;
            while (Control.doAutoLevel && !Control.IsLevel && remaining > 0)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }
            Log.Write("Archon is level or deadline has passed");
            autoLevelRoutine = null;
            if (Control.doAutoLevel)
            {
                Log.Write("Archon leveling has not been aborted");
                Control.doAutoLevel = false;
                if (Control.IsLevel)
                {
                    Log.Write("Archon is level. Exiting");
                    Logger.PDANote($"{VehicleName} is level. Exiting");
                    base.DeselectSlots();
                }
                else
                {
                    Log.Write("Archon is not level. Not exiting");
                    Logger.PDANote($"Failed to auto-level in {timewindow} seconds. Cannot exit here. Please navigate to an area where the {VehicleName} can level out and try again.");
                }
            }
        }


        private void LazyInit()
        {
            if (!isInitialized)
            {
                Log.Write($"LocalInit() first time");
                isInitialized = true;
                try
                {

                    //if (autopilot)
                    //{
                    //    //"Airon" - weird, partially indecipherable low energy voice
                    //    //"Chels-E" - high-pitched panicky
                    //    //"Mikjaw"/"Salli" - just bad
                    //    //"Turtle" - missing?
                    //    //autopilot.apVoice.voice = VoiceManager.GetVoice("Salli");

                    //    //var source = autopilot.apVoice.voice;
                    //    //var source = VoiceManager.GetVoice("ShirubaFoxy");
                    //    //autopilot.apVoice.voice = Helper.Clone(source);
                    //    //autopilot.apVoice.voice.PowerLow = null;
                    //    //autopilot.apVoice.voice.BatteriesNearlyEmpty = null;
                    //    //autopilot.apVoice.voice.UhOh = null;

                    //}

                    energyInterface = GetComponent<EnergyInterface>();
                    control = GetComponent<ArchonControl>();
                    //var loadSave = gameObject.GetComponent<LoadSaveComponent>();
                    //if (!loadSave)
                    //    loadSave = gameObject.AddComponent<LoadSaveComponent>();
                    //loadSave.control = control;

                    //rotateCamera = GetComponentInChildren<RotateCamera>();

                    //if (rotateCamera == null)
                    //    EchLog.Write($"Rotate camera not found");
                    //else
                    //    EchLog.Write($"Found camera rotate {rotateCamera.name}");
                    Control.RedetectDocked();
                    if (control != null)
                    {
                        Log.Write("Found control");
                    }
                    else
                    {
                        if (transform == null)
                            Log.Write($"Do not have a transform");
                        else
                        {
                            Log.Write($"This is {transform.name}");
                            Log.Write("This has components: " + Helper.NamesS(Helper.AllComponents(transform)));
                            Log.Write("This has children: " + Helper.NamesS(Helper.Children(transform)));
                        }
                    }
                    Log.Write($"LocalInit() done");

                }
                catch (Exception e)
                {
                    Log.Error("LocalInit()", e);
                }

            }
        }


        public override void SetBaseColor(VehicleColor color)
        {
            Log.Write($"Updating sub base color to {color}");
            base.SetBaseColor(color);

            var listeners = GetComponentsInChildren<IColorListener>();
            foreach (var listener in listeners)
                listener.SetColors(BaseColor.RGB, StripeColor.RGB);

        }

        public override void SetStripeColor(VehicleColor color)
        {
            Log.Write($"Updating sub stripe color to {color}");
            base.SetStripeColor(color);

            var listeners = GetComponentsInChildren<IColorListener>();
            foreach (var listener in listeners)
                listener.SetColors(BaseColor.RGB, StripeColor.RGB);
        }


        public override void Start()
        {
            try
            {
                Log.Write(nameof(Start));


                LazyInit();

                base.Start();
                Log.Write(nameof(Start) + " done");

            }
            catch (Exception ex)
            {
                Log.Error(nameof(Start), ex);
            }
        }



        protected override void OnPrePlayerEntry()
        {
            Log.Write(nameof(PlayerEntry));
            Control.Enter(Helper.GetPlayerReference(), skipOrientation: exitLimitsSuspended || !hadUnpausedFrame);
            HudPingInstance.SetHudIcon(false);

            base.OnPrePlayerEntry();
        }

        protected override void OnPlayerExit()
        {
            base.OnPlayerExit();
            HudPingInstance.SetHudIcon(true);
            Control.Exit();

        }



        protected override void OnPreBeginHelmControl(Helm helm)
        {
            Log.Write(nameof(OnPreBeginHelmControl));
            try
            {
                base.OnPreBeginHelmControl(helm);
                //if (!liveMixin.IsAlive() || wasDead)
                //{
                //    ErrorMessage.AddError(string.Format(Language.main.Get("destroyedAndCannotBeBoarded"), VehicleName));
                //    return;
                //}
                //if (refreshQuickslotsOnControl.HasValue)
                //{
                //    var v = refreshQuickslotsOnControl.Value;
                //    Control.PrepareForSaving();
                //    refreshQuickslotsOnControl = null;
                //    //SignalQuickslotsChangedWhilePiloting(v);
                //}

            }
            catch (Exception ex)
            {
                Log.Error(nameof(OnPreBeginHelmControl), ex);
            }
        }

        protected override void OnBeginHelmControl(Helm helm)
        {
            Log.Write(nameof(OnBeginHelmControl));
            try
            {
                base.OnBeginHelmControl(helm);
                LazyInit();

                Control.Control(Helper.GetPlayerReference());
            }
            catch (Exception ex)
            {
                Log.Error(nameof(OnBeginHelmControl), ex);
            }

        }

        protected override void OnPreEndHelmControl()
        {
            try
            {
                Log.Write(nameof(OnPreEndHelmControl));

                LazyInit();
                Control.ExitControl(Helper.GetPlayerReference(), skipOrientation: exitLimitsSuspended);
            }
            catch (Exception ex)
            {
                Log.Error(nameof(OnPreEndHelmControl), ex);
            }
        }

        protected override void OnEndHelmControl()
        {
            try
            {
                Log.Write(nameof(OnEndHelmControl));

                base.OnEndHelmControl();

                if (Player.main.sitting)
                {
                    Log.Error($"Player is still sitting after control exit");
                    Player.main.sitting = false;
                    Player.main.playerController.ForceControllerSize();
                }
                else
                    Log.Write($"Sitting not detected");

                Player.main.transform.LookAt(transform.position);

            }
            catch (Exception ex)
            {
                Log.Error(nameof(OnEndHelmControl), ex);
            }
        }

        private bool fixedUpdateError = false;
        private bool wasAboveWater = false;

        private PARAMETER_ID verticalVelocitySoundIndex = FMODUWE.invalidParameterId;
        private void PlaySplashSound()
        {
            EventInstance ev = FMODUWE.GetEvent(splashSound);
            ev.set3DAttributes(base.transform.position.To3DAttributes());
            if (FMODUWE.IsInvalidParameterId(verticalVelocitySoundIndex))
            {
                verticalVelocitySoundIndex = FMODUWE.GetEventInstanceParameterIndex(ev, "verticalVelocity");
            }

            ev.setParameterValueByIndex(verticalVelocitySoundIndex, useRigidbody.velocity.y);
            ev.start();
            ev.release();
        }

        private void SetWaterProxiesEnabled(bool enable)
        {
            var clipProxyParent = transform.Find("WaterClipProxy");
            var seamoth = PrefabLoader.Request(TechType.Seamoth).Instance;
            if (seamoth == null)
            {
                Log.Write("Seamoth prefab not found. Can't adjust clip proxies right now");
                return;
            }
            if (clipProxyParent && seamoth != null)
            {
                WaterClipProxy seamothWCP = seamoth.GetComponentInChildren<WaterClipProxy>();

                for (int i = 0; i < clipProxyParent.childCount; i++)
                {
                    var go = clipProxyParent.GetChild(i).gameObject;
                    foreach (var c in go.GetComponents<Component>())    //clear out anything. Even if disabled, this blocks usage
                        if (!(c is Transform))
                            Destroy(c);

                    if (enable)
                    {
                        WaterClipProxy waterClip = go.AddComponent<WaterClipProxy>();
                        waterClip.shape = WaterClipProxy.Shape.Box;
                        //"""Apply the seamoth's clip material. No idea what shader it uses or what settings it actually has, so this is an easier option. Reuse the game's assets.""" -Lee23
                        waterClip.clipMaterial = seamothWCP.clipMaterial;
                        //"""You need to do this. By default the layer is 0. This makes it displace everything in the default rendering layer. We only want to displace water.""" -Lee23
                        waterClip.gameObject.layer = seamothWCP.gameObject.layer;
                    }
                }
                clippingWater = enable;
                Log.Write($"Water-clip proxies adapted ({enable} ({ClipWaterS}))");

            }
            else
                Log.Write("Clip proxies or seamoth not found. Can't adjust right now");
        }

        public bool ClipWater => Control.CameraIsInVehicle && !Control.BoardedByHeadless;
        public string ClipWaterS => $"CameraIsInVehicle={Control.CameraIsInVehicle} && !(BoardedByHeadless = {Control.BoardedByHeadless})";


        public override void FixedUpdate()
        {
            try
            {
                LazyInit();


                stabilizeRoll = false;

                if (worldForces.IsAboveWater() != wasAboveWater)
                {
                    PlaySplashSound();
                    wasAboveWater = worldForces.IsAboveWater();
                }

                prevVelocity = useRigidbody.velocity;
                //base.FixedUpdate();
            }
            catch (Exception ex)
            {
                if (!fixedUpdateError)
                {
                    fixedUpdateError = true;
                    Log.Error(nameof(FixedUpdate), ex);
                }
            }
        }

        private void ProcessEnergyRecharge()
        {

            if (energyInterface != null)
            {
                //                var batteryMk = GetBatteryMark();

                //float level = 1;

                //float recharge =
                //      0.4f  //max 1.6 per second
                //    * level;

                //energyInterface.ModifyCharge(
                //    Time.deltaTime
                //    * recharge
                //    );
                energyInterface.GetValues(out var energyCharge, out var energyCapacity);
                Control.currentEnergy = energyCharge;
                Control.maxEnergy = energyCapacity;


            }
        }

        private void ProcessRegeneration()
        {
            Control.isHealing = false;

            var delta = Time.deltaTime;

            if (liveMixin != null)
            {

                if (delta > 0)
                {

                    var criticalHealingLimit = liveMixin.maxHealth * 0.05f;
                    var critical = liveMixin.health < liveMixin.maxHealth * 0.01f;
                    if (critical && !isInCriticalRecovery)
                    {
                        ErrorMessage.AddMessage(Language.main.Get($"CriticalHealth.RepairEnabled"));
                        Log.Warn($"Vehicle at critical health. Reviving. Setting invincible. Enabling emergency self healing");
                        liveMixin.invincible = true;
                        isInCriticalRecovery = true;
                    }

                    if (liveMixin.health < criticalHealingLimit && isInCriticalRecovery)
                    {
                        var healing = liveMixin.maxHealth
                            * delta
                            * 0.01f;
                        var clamped = Mathf.Min(healing, criticalHealingLimit - liveMixin.health);
                        var effective = clamped / healing;
                        //Debug.Log($"Healing at delta={Time.deltaTime}");
                        float energyDemand =
                            20
                            * delta
                            //* MainPatcher.PluginConfig.selfHealingSpeed / 100   //if slower, cost less
                            * effective //if clamped, cost less
                            ;
                        PowerManager.TrySpendEnergy(energyDemand);

                        liveMixin.AddHealth(clamped);
                        Control.isHealing = true;

                    }
                    else if (isInCriticalRecovery)
                    {
                        ErrorMessage.AddMessage(Language.main.Get($"CriticalHealth.RepairDone"));

                        Log.Warn($"Emergency healing concluded switching off");
                        isInCriticalRecovery = false;
                        liveMixin.invincible = false;
                    }


                    else if (!Control.batteryDead)
                    {
                        float level = RepairModule.GetRelativeSelfRepair(RepairModule.GetFrom(this));

                        if (liveMixin.health < liveMixin.maxHealth && level > 0)
                        {
                            var healing = liveMixin.maxHealth
                                * delta
                                * level
                                //* 0.02f //max = 2% of max health per second
                                //* MainPatcher.PluginConfig.selfHealingSpeed / 100   //default will be 5 seconds per 1%
                                ;

                            var clamped = Mathf.Min(healing, liveMixin.maxHealth - liveMixin.health);
                            var effective = clamped / healing;
                            //Debug.Log($"Healing at delta={Time.deltaTime}");
                            float energyDemand =
                                10
                                * delta
                                //* MainPatcher.PluginConfig.selfHealingSpeed / 100   //if slower, cost less
                                * effective //if clamped, cost less
                                ;

                            PowerManager.TrySpendEnergy(energyDemand);
                            var actuallyHealed = clamped;
                            liveMixin.AddHealth(actuallyHealed);
                            Control.isHealing = true;
                        }
                    }
                }
                Control.maxHealth = liveMixin.maxHealth;
                Control.currentHealth = liveMixin.health;

            }
        }

        private void ForwardControlAxes()
        {
            if (Control.batteryDead || Control.powerOff || engine == null)
            {
                Control.forwardAxis = 0;
                Control.rightAxis = 0;
                Control.upAxis = 0;
            }
            else
            {
                Control.forwardAxis = engine.currentInput.z;
                Control.rightAxis = engine.currentInput.x;
                Control.upAxis = engine.currentInput.y;
            }
        }

        private void ProcessEngine()
        {
            if (engine == null)
            {
                return;
            }
            engine.overdriveActive = 0;
            engine.doNotAccelerate = Control.doAutoLevel || Control.batteryDead;
            engine.freeCamera = Control.freeCamera;
            //return;

            //var boostToggle = false;// !MainPatcher.PluginConfig.holdToBoost;

            ////engine.driveUpgrade = HighestModuleType(ArchonModule.DriveMk1, ArchonModule.DriveMk2, ArchonModule.DriveMk3);

            ////if (GameInput.GetButtonDown(GameInput.Button.Sprint) && boostToggle)
            ////{
            ////    if (control.forwardAxis > 0 && engine.overdriveActive > 0)
            ////        engine.overdriveActive = 0;
            ////}

            //bool canBoost =
            //    !lowPower
            //    ;

            //if (boostToggle)
            //{
            //    if (control.forwardAxis <= 0 || !canBoost)
            //        engine.overdriveActive = 0;
            //    else
            //        engine.overdriveActive = Mathf.Max(engine.overdriveActive, GameInput.GetAnalogValueForButton(GameInput.Button.Sprint));
            //}
            //else
            //    engine.overdriveActive = control.forwardAxis > 0 && canBoost
            //        ? GameInput.GetAnalogValueForButton(GameInput.Button.Sprint)
            //        : 0;


            //control.overdriveActive = engine.overdriveActive > 0.5f;
        }

        private void ProcessTriggers()
        {
            if (Control.IsBeingControlled
                && Player.main.pda.state == PDA.State.Closed
                && !IngameMenu.main.gameObject.activeSelf
                )
            {
                if (GameInput.GetButtonDown(GameInput.Button.RightHand))
                {
                    SetLights(!Control.lights);
                }
            }

        }

        private void SetLights(bool on)
        {
            if (Control.lights == on)
                return;
            if (on && !Control.lights && Control.batteryDead)
            {
                Log.Warn($"Battery dead. Cannot turn lights on");
                return;
            }
            Control.lights = on;
            if (on)
            {
                LightsOnSound.Stop();
                LightsOnSound.Play();
            }
            else
            {
                LightsOffSound.Stop();
                LightsOffSound.Play();
            }
        }

        /// <summary>
        /// Redetects proximity to the ocean surface and forwards the state to control
        /// </summary>
        private void RepositionCamera()
        {
            Control.UpdateLowCamera(Ocean.GetOceanLevel());
        }

        private bool HasModule(ArchonModule module)
            => moduleCounts[(int)module] > 0;

        private int HighestModule(params ArchonModule[] m)
        {
            for (int i = m.Length - 1; i >= 0; i--)
                if (HasModule(m[i]))
                    return i + 1;
            return 0;
        }

        public ArchonModule HighestModuleType(params ArchonModule[] m)
        {
            for (int i = m.Length - 1; i >= 0; i--)
                if (HasModule(m[i]))
                    return m[i];
            return ArchonModule.None;
        }


        public override void OnVehicleUndocked()
        {
            base.OnVehicleUndocked();
            //MaterialFixer.OnVehicleUndocked();
        }


        //private MaterialFixer MaterialFixer;

        private Color nonBlackBaseColor;
        private Color nonBlackStripeColor;

        //public override void OnVehicleDocked(Vehicle vehicle, Vector3 exitLocation)
        //{
        //    base.OnVehicleDocked(vehicle, exitLocation);
        //    SetBaseColor(Vector3.zero, nonBlackBaseColor);
        //    SetStripeColor(Vector3.zero, nonBlackStripeColor);
        //}

        private static float SecondaryEulerZeroDistance(float euler)
        {
            return euler > 180f
                ? 360f - euler  //mirror around
                : euler;
        }


        private MenuTracker MenuTracker { get; } = new MenuTracker();

        private IEnumerator ReenableColliders()
        {
            yield return new WaitForSeconds(0.1f);
            Log.Write("Reenabling colliders");

            Control.interiorColliders.gameObject.SetActive(true);
        }


        public override void Update()
        {
            try
            {
                LazyInit();


                if (clippingWater != ClipWater)
                {
                    SetWaterProxiesEnabled(ClipWater);
                }

                MenuTracker.Update();
                hadUnpausedFrame |= Time.deltaTime > 0;


                //if (Player.main.sitting)
                //{
                //    Log.Error($"Player is sitting in sub");
                //    Player.main.sitting = false;
                //    Player.main.playerController.ForceControllerSize();
                //}

                if (BaseColor.RGB != Color.black)
                    nonBlackBaseColor = BaseColor.RGB;
                if (StripeColor.RGB != Color.black)
                    nonBlackStripeColor = StripeColor.RGB;

                //MaterialFixer.OnUpdate();

                Control.flipFreeHorizontalRotationInReverse = MainPatcher.PluginConfig.flipFreeHorizontalRotationInReverse;
                Control.flipFreeVerticalRotationInReverse = MainPatcher.PluginConfig.flipFreeVerticalRotationInReverse;

                if (Input.GetKeyDown(KeyCode.F7))
                {
                    //if (Player.main.currentMountedVehicle != null)
                    //{
                    //    HierarchyAnalyzer a = new HierarchyAnalyzer();
                    //    a.LogToJson(Player.main.currentMountedVehicle.transform, $@"C:\temp\vehicle.json");
                    //}

                    //Log.Write($"Reapplying materials");
                    //MaterialFixer.ReApply();

                    /*
                     * Concerning the loss of buildability when entering certain areas around the nose.
                     * -) When it happens no currently logged events are triggered, neither by the Archon nor by any external routine
                     * -) There is no loss of other functionality. The player can still walk but no building can be performed
                     * -) Exiting and re-entering the vehicle fixes the state
                     * -) Switching off interior colliders and re-enabling them after 10ms is noticable but does not fix the state
                     * -) Calling base.PlayerExit(), then immediately base.PlayerEntry() fixes it only if the player is
                     *      not in something of a deadzone where building is always terminated. Only the player's location is relevant,
                     *      where they aim at can be outside this zone.
                     */
                    if (Control.IsBoardedButNotControlled)
                    {
                        Log.Write("Debug action");
                        Log.Write($"@{transform.position}");
                        //TryFixLostBuildFocus();
                        //Control.interiorColliders.gameObject.SetActive(false);
                        //StartCoroutine(ReenableColliders());
                    }
                }




                if (!liveMixin.IsAlive() || wasDead)
                {
                    Log.Warn($"Vehicle reported as dead. Reviving. Setting invincible");
                    wasDead = false;
                    liveMixin.health = liveMixin.maxHealth * 0.01f;
                    liveMixin.invincible = true;    //archon is immortal
                    //                    wasDead = true;
                    //deathAge += Time.deltaTime;
                    //if (deathAge > 1.5f)
                    //{
                    //    Log.Write($"Emitting pseudo self destruct");
                    //    Control.SelfDestruct(true);
                    //    Log.Write($"Calling OnSalvage");
                    //    OnSalvage();
                    //    enabled = false;
                    //    Log.Write($"Done?");
                    //    return;
                    //}
                }

                //ArchonControl.targetArrows = MainPatcher.PluginConfig.targetArrows;


                Vector2 lookDelta = GameInput.GetLookDelta();

                if (Character.IsAnyMenuOpen)
                    Control.lookRightAxis = Control.lookUpAxis = 0;
                else
                {
                    Control.lookRightAxis = lookDelta.x * 0.1f;
                    Control.lookUpAxis = lookDelta.y * 0.1f;
                }

                ProcessEnergyRecharge();
                ProcessRegeneration();
                ForwardControlAxes();
                ProcessTriggers();

                Control.outOfWater = !GetIsUnderwater();
                GetDepth(out var depth, out var crush);
                Control.environmentalLeanIntensity = LeanIntensityCalculator.CalculateLeanIntensity(
                    depth,
                    useRigidbody.velocity.magnitude,
                    30);
                Control.forceCockpitCamera = Player.main.pda.state == PDA.State.Opened;

                if (Player.main.pda.state == PDA.State.Closed && !IngameMenu.main.gameObject.activeSelf)
                {
                    Control.zoomAxis = -Input.GetAxis("Mouse ScrollWheel")
                        +
                        ((Input.GetKey(MainPatcher.PluginConfig.altZoomOut) ? 1f : 0f)
                        - (Input.GetKey(MainPatcher.PluginConfig.altZoomIn) ? 1f : 0f)) * 0.02f
                        ;
                }

                if (Control.IsBeingControlled
                    && GameInput.GetKeyDown(MainPatcher.PluginConfig.toggleFreeCamera)
                    && engine != null)
                    engine.freeCamera = Control.freeCamera = !Control.freeCamera;

                ProcessEngine();
                RepositionCamera();

                if (energyInterface != null)
                {
                    energyInterface.GetValues(out var energyCharge, out var energyCapacity);

                    Control.maxEnergy = energyCapacity;
                    Control.currentEnergy = energyCharge;
                }

                base.Update();
            }
            catch (Exception ex)
            {
                Log.Error(nameof(Update), ex);
            }
        }


        public void OnPowerUp()
        {
            Control.powerOff = false;
        }

        public void OnPowerDown()
        {
            Control.powerOff = true;
        }

        public void OnBatteryDead()
        {
            Control.batteryDead = true;
            SetLights(false);
        }

        public void OnBatteryRevive()
        {
            Control.batteryDead = false;
            SetLights(true);
        }

        public void OnBatterySafe()
        {
        }

        public void OnBatteryLow()
        {
        }

        public void OnBatteryNearlyEmpty()
        {
        }

        public void OnBatteryDepleted()
        {
        }

        internal void SetModuleCount(ArchonModule moduleType, int count)
        {
            //var tm = GetTorpedoMark();
            //var bm = GetBatteryMark();
            //var dm = GetDriveMark();
            var rm = RepairModule.GetFrom(this);
            moduleCounts[(int)moduleType] = count;
            //var tm2 = GetTorpedoMark();
            //var bm2 = GetBatteryMark();
            //var dm2 = GetDriveMark();
            var rm2 = RepairModule.GetFrom(this);
            if (!destroyed && hadUnpausedFrame)
            {
                //if (tm != tm2)
                //    ErrorMessage.AddMessage(string.Format(Language.main.Get($"torpedoCapChanged"), VehicleName, Language.main.Get("cap_t_" + tm2)));
                //if (bm != bm2)
                //    ErrorMessage.AddMessage(string.Format(Language.main.Get($"batteryCapChanged"), VehicleName, Language.main.Get("cap_b_" + bm2)));
                //if (dm != dm2)
                //    ErrorMessage.AddMessage(string.Format(Language.main.Get($"boostCapChanged"), VehicleName, Language.main.Get("cap_d_" + dm2)));
                if (rm != rm2)
                    ErrorMessage.AddMessage(Language.main.GetFormat($"repairCapChanged", VehicleName, Language.main.Get("cap_r_" + rm2)));
            }
            Debug.Log($"Changed counts of {moduleType} to {moduleCounts[(int)moduleType]}");
        }

        internal void EnterFromDocking()
        {
            Log.Write(nameof(EnterFromDocking));
            SuspendAutoLeveling();

            var dockingHatchEntry = transform.Find("Docking Hatch/Exit");
            if (dockingHatchEntry)
            {
                Log.Write($"Docking hatch entry found at {dockingHatchEntry.position}");
                PlayerEntry(new VehicleHatchDefinition(gameObject, dockingHatchEntry, dockingHatchEntry, dockingHatchEntry));
                //Player.main.transform.position = dockingHatchEntry.position;
                //Player.main.transform.rotation = dockingHatchEntry.rotation;
            }
            else
            {
                Log.Error($"Docking hatch entry not found. Entering helm");
                ClosestPlayerEntry();
                BeginHelmControl(Com.Helms[0]);
            }
            AnticipatePlayerIssues = true;

            RestoreAutoLeveling();

        }

        //public float ExitPitchLimit
        //    => exitLimitsSuspended
        //        ? 360
        //        : base.ExitPitchLimit;

        //public float ExitRollLimit
        //    => exitLimitsSuspended
        //        ? 360
        //        : base.ExitRollLimit;

        private bool exitLimitsSuspended = false;
        internal void SuspendAutoLeveling()
        {
            exitLimitsSuspended = true;
        }
        internal void RestoreAutoLeveling()
        {
            exitLimitsSuspended = false;
        }

        //public void ToggleSlot(QuickSlot slot, bool enabled)
        //{
        //    base.ToggleSlot(slot.Index, enabled);
        //}

        private readonly Undoable disabledCameras = new Undoable();
        //private QuickSlot? refreshQuickslotsOnControl;
        //internal void SignalQuickslotsChangedWhileLoading(QuickSlot slot)
        //{
        //    refreshQuickslotsOnControl = slot;
        //}
        //internal void SignalQuickslotsChangedWhilePiloting(QuickSlot slot)
        //{
        //    Log.Write(nameof(SignalQuickslotsChangedWhilePiloting));
        //    if (!Control.IsBeingControlled)
        //    {
        //        Log.Write($"Not actually piloting. Ignoring");
        //        return;
        //    }
        //    //var qs = uGUI.main.quickSlots;
        //    //new MethodAdapter<uGUI_ItemIcon, TechType>(qs, "SetForeground")
        //    //    .Invoke(qs.GetIcon(slot.Index), TechType.None);
        //    //new MethodAdapter<uGUI_ItemIcon, TechType, bool>(qs, "SetBackground")
        //    //    .Invoke(qs.GetIcon(slot.Index), TechType.None, false);

        //    SuspendAutoLeveling();
        //    base.DeselectSlots();
        //    RestoreAutoLeveling();
        //    //foreach (var mbehavior in GetComponentsInChildren<MonoBehaviour>())
        //    //    SimulateUpdate(mbehavior);
        //    //foreach (var mbehavior in Player.main.GetComponentsInChildren<MonoBehaviour>())
        //    //    SimulateUpdate(mbehavior);
        //    //BeginPiloting();

        //    Player.main.camRoot
        //        .GetComponentsInChildren<Camera>()
        //        .ToEnabled()
        //        .DisableAllEnabled(disabledCameras);
        //    StartCoroutine(ReenterNextFrame());
        //}

        private IEnumerator ReenterNextFrame()
        {
            yield return null;
            BeginHelmControl(Com.Helms[0]);
            disabledCameras.UndoAll();
        }


        public override string vehicleDefaultName => "Archon";

        /// <summary>
        /// The prefab IDs of submarines declared docked during saving, restored during loading.
        /// </summary>
        public IReadOnlyList<string>? DockedSubPrefabIds { get; private set; }

        protected override SubmarineComposition GetSubmarineComposition()
        {
            var voiceLibraries = transform.GetComponentsInChildren<VoiceLibrary>();
            if (voiceLibraries.Length == 0)
            {
                Log.Error("Voice libraries not found. Autopilot will not have a voice");
            }
            else
            {
                foreach (var voiceLibrary in voiceLibraries)
                {
                    Log.Write($"Registering voice library {voiceLibrary.voiceName}");
                    VoiceLibraries[voiceLibrary.voiceName] = voiceLibrary;
                }
            }


            var hatches = transform.Find("Hatches");
            var hatchList = new List<VehicleHatchDefinition>();
            if (hatches)
            {
                foreach (Transform hatch in hatches)
                {
                    var exit = hatch.Find("Exit");
                    var entry = hatch.Find("Entry");
                    if (!exit || !entry)
                    {
                        Log.Error("Hatch children not found of " + hatch);
                        continue;
                    }
                    hatchList.Add(new VehicleHatchDefinition(
                        hatch: hatch.gameObject,
                        exit: exit,
                        surfaceExit: exit,
                        entry: entry)
                    );
                }
                Log.Write($"Detected {hatchList.Count} hatch(es)");
            }

            var arButtons = transform.GetComponentsInChildren<ArButton>(true);
            foreach (var arButton in arButtons)
            {
                Log.Write($"Found AR button {arButton.name} at {arButton.transform.position}");
                var helper = arButton.gameObject.EnsureComponent<ArchonArButton>();
                helper.arButton = arButton;
            }

            var storageRootTransform = transform.Find("StorageRoot");
            if (storageRootTransform == null)
            {
                Log.Write($"Storage root not found. Creating new one");
                storageRootTransform = new GameObject("StorageRoot").transform;
                storageRootTransform.parent = transform;
                storageRootTransform.localPosition = Vector3.zero;
            }
            else
            {
                Log.Write($"Found storage root {storageRootTransform}");
            }


            var modularStorageList = new List<VehicleStorage>();
            if (storageRootTransform)
            {
                for (int i = 0; i < 8; i++)
                {
                    var name = $"Storage{i}";
                    var storageTransform = storageRootTransform.Find(name);
                    if (storageTransform == null)
                    {
                        storageTransform = new GameObject(name).transform;
                        storageTransform.parent = storageRootTransform.transform;
                        storageTransform.localPosition = M.V3(i);
                        Log.Write($"Creating new storage transform {storageTransform} in {storageRootTransform} @{storageTransform.localPosition} => {storageTransform.position}");
                    }
                    modularStorageList.Add(new VehicleStorage(
                        displayName: AVS.Localization.Text.Translated("Component.ModularStorage"),
                        container: storageTransform.gameObject,
                        height: 2,
                        width: 2
                        )
                    );
                }
            }


            var innateStorages = new List<VehicleStorage>();
            var waterTank = transform.Find("Interior/Water Tank");
            if (waterTank != null)
            {
                innateStorages.Add(new VehicleStorage(
                    displayName: AVS.Localization.Text.Translated("Component.WaterTank"),
                    container: waterTank.gameObject,
                    height: 10,
                    width: 8
                ));
            }
            else
            {
                Log.Write($"Water tank not found");
            }

            List<GameObject> waterClipProxies = new List<GameObject>();
            var clipProxies = transform.Find("WaterClipProxy");
            foreach (Transform proxy in clipProxies)
            {
                var go = proxy.gameObject;
                Destroy(go.GetComponent<MeshRenderer>());
                Destroy(go.GetComponent<MeshFilter>());
                waterClipProxies.Add(go);
            }

            var upgrades = new List<VehicleUpgrades>();
            var ui = transform.Find("Interior/Upgrade Panel");
            var plugs = transform.Find("Module Plugs");

            var plugProxies = new List<Transform>();
            if (plugs)
            {
                for (int i = 0; i < plugs.childCount; i++)
                {
                    var plug = plugs.GetChild(i);
                    var position = plug.Find("Module Position");
                    if (position != null)
                        plugProxies.Add(position);
                    else
                        Log.Write($"Plug {plug.name} does not have a 'Module Position' child");
                }
            }
            else
                Log.Write($"Plugs not found");

            Log.Write($"Determined {plugProxies.Count} plug(s)");

            if (ui)
            {
                upgrades.Add(new VehicleUpgrades(
                    @interface: ui.gameObject,
                    flap: ui.gameObject,
                    Vector3.zero, //ui flap position
                    Vector3.zero, //ui flap rotation
                    plugProxies
                ));
            }
            else
                Log.Write($"Upgrades interface not found");

            var vehicleBatteries = new List<VehicleBattery>();


            var batteries = transform.Find("Batteries");

            if (batteries)
            {
                for (int i = 0; i < batteries.childCount; i++)
                {
                    var b = batteries.GetChild(i);
                    if (b != null)
                    {
                        vehicleBatteries.Add(new VehicleBattery(
                            batterySlot: b.gameObject,
                            batteryProxy: b
                        ));
                    }
                }
            }
            else
                Log.Write($"Unable to locate 'Batteries' child");

            var helms = new List<Helm>();
            var helm = transform.Find("Helm");
            if (helm)
            {
                var helmExit = helm.Find($"ExitLocation");
                if (!helmExit)
                    Log.Write($"Helm exit not found for {helm.NiceName()}");

                helms.Add(new Helm
                (
                    root: helm.gameObject,
                    playerControlLocation: helm.gameObject,
                    exitLocation: helmExit,
                    isSeated: true
                ));
            }
            else
                Log.Error("Helm not found");

            var tetherSources = new List<GameObject>();
            var tether = transform.Find("Tether");
            if (!tether)
            {

                Log.Error("Tether not found. No tethers will be defined");

            }
            else
            {
                foreach (Transform trans in tether)
                {
                    var t = trans.GetComponent<SphereCollider>();
                    if (!t)
                    {
                        Log.Error($"Tether {trans} does not hace a sphere collider");
                        continue;
                    }
                    t.radius = t.transform.localScale.x;
                    t.transform.localScale = Vector3.one;
                    tetherSources.Add(t.gameObject);
                }
                Log.Write($"Recorded {tetherSources.Count} tether source(s)");
            }





            Log.Write($"Assigned new engine");
            engine = gameObject.EnsureComponent<MassDrive>();





            return new SubmarineComposition(
                engine: engine,
                hatches: hatchList,
                collisionModel: transform.Find("CollisionModel").gameObject,
                boundingBoxCollider: transform.Find("EntireBoundingBox").GetComponent<BoxCollider>(),
                storageRootObject: storageRootTransform.gameObject,
                modularStorages: modularStorageList,
                innateStorages: innateStorages,
                waterClipProxies: waterClipProxies,
                upgrades: upgrades,
                batteries: vehicleBatteries,
                tetherSources: tetherSources,
                modulesRootObject: GetOrCreateDefaultModulesRootObject(),
                helms: helms
                );




        }

        void IAutopilotEventListener.Signal(AutopilotEvent autopilotEvent)
        {

            Log.Write($"Received autopilot event {autopilotEvent}");
            return;

            switch (autopilotEvent)
            {
                case AutopilotEvent.PlayerEntry:
                    {
                        if (VoiceLibraries.TryGetValue(MainPatcher.PluginConfig.voice.ToString(), out var voiceLibrary))
                        {
                            var voices = voiceLibrary.GetRandomWelcome(out var isCombined).ToList();
                            List<float> gaps = new List<float>();

                            if (voices != null)
                            {
                                for (int i = 0; i + 1 < voices.Count; i++)
                                {
                                    gaps.Add(0.1f);
                                }
                                if (Autopilot.HealthStatus == AutopilotStatus.HealthSafe
                                    && Autopilot.PowerStatus == AutopilotStatus.PowerSafe
                                    && Autopilot.DepthStatus == AutopilotStatus.DepthSafe

                                    )
                                {
                                    if (!isCombined) //combined welcome does not blend well with status green voice
                                    {
                                        gaps.Add(1);
                                        voices.Add(voiceLibrary.GetRandomAllSystemsGreen());
                                    }
                                }
                                else
                                {
                                    gaps.Add(1);
                                    switch (Autopilot.HealthStatus)
                                    {
                                        case AutopilotStatus.HealthCritical:
                                            voices.Add(voiceLibrary.GetRandomHealthCritical());
                                            break;
                                        case AutopilotStatus.HealthLow:
                                            voices.Add(voiceLibrary.GetRandomHealthLow());
                                            break;
                                    }
                                    switch (Autopilot.PowerStatus)
                                    {
                                        case AutopilotStatus.PowerCritical:
                                            voices.Add(voiceLibrary.GetRandomPowerCritical());
                                            break;
                                        case AutopilotStatus.PowerLow:
                                            voices.Add(voiceLibrary.GetRandomPowerLow());
                                            break;
                                    }
                                    switch (Autopilot.DepthStatus)
                                    {
                                        case AutopilotStatus.DepthBeyondCrush:
                                            voices.AddRange(voiceLibrary.GetRandomDepthCritical());
                                            break;
                                        case AutopilotStatus.DepthNearCrush:
                                            voices.Add(voiceLibrary.GetRandomDepthDangerous());
                                            break;
                                    }
                                }
                                VoiceQueue.Play(new VoiceLine(voices, gaps, "voiceWelcome", 0));
                            }
                            else
                                Log.Error("Voice for PlayerEntry not found");
                        }
                        else
                            Log.Error($"Voice library {MainPatcher.PluginConfig.voice} not found");
                    }
                    break;
            }
        }

        void IAutopilotEventListener.Signal(AutopilotStatusChange statusChange)
        {
            Log.Write($"Received autopilot event {statusChange.NewStatus}");

            return;
            if (VoiceLibraries.TryGetValue(MainPatcher.PluginConfig.voice.ToString(), out var voiceLibrary))
            {
                switch (statusChange.NewStatus)
                {
                    case AutopilotStatus.DepthNearCrush:
                        if (statusChange.PreviousStatus < AutopilotStatus.DepthNearCrush)
                        {
                            var voice = voiceLibrary.GetRandomDepthDangerous();
                            if (voice)
                            {
                                VoiceQueue.Play(new VoiceLine(voice, "voiceDepthDangerous", 1));
                            }
                            else
                                Log.Error("Voice for DepthNearCrush not found");
                        }
                        break;
                    case AutopilotStatus.DepthBeyondCrush:
                        if (statusChange.PreviousStatus < AutopilotStatus.DepthBeyondCrush)
                        {
                            var voices = voiceLibrary.GetRandomDepthCritical();
                            if (voices != null)
                            {
                                VoiceQueue.Play(new VoiceLine(voices, null, "voiceDepthCritical", 2));
                            }
                            else
                                Log.Error("Voice for DepthBeyondCrush not found");
                        }
                        break;
                    case AutopilotStatus.HealthCritical:
                        if (statusChange.PreviousStatus < AutopilotStatus.HealthCritical)
                        {
                            var voice = voiceLibrary.GetRandomHealthCritical();
                            if (voice)
                            {
                                VoiceQueue.Play(new VoiceLine(voice, "voiceHealthCritical", 2));
                            }
                            else
                                Log.Error("Voice for HealthCritical not found");
                        }
                        break;
                    case AutopilotStatus.HealthLow:
                        if (statusChange.PreviousStatus < AutopilotStatus.HealthLow)
                        {
                            var voice = voiceLibrary.GetRandomHealthLow();
                            if (voice)
                            {
                                VoiceQueue.Play(new VoiceLine(voice, "voiceHealthLow", 1));
                            }
                            else
                                Log.Error("Voice for HealthLow not found");
                        }
                        break;
                    case AutopilotStatus.LeviathanNearby:
                        //if (statusChange.PreviousStatus < AutopilotStatus.LeviathanNearby)
                        //{
                        //    var voice = voiceLibrary.GetRandomLeviathanNearby();
                        //    if (voice)
                        //    {
                        //        VoiceQueue.Play(new VoiceLine(voice, "voiceLeviathanNearby", 1));
                        //    }
                        //    else
                        //        Log.Error("Voice for LeviathanNearby not found");
                        //}
                        break;
                    case AutopilotStatus.PowerLow:
                        if (statusChange.PreviousStatus < AutopilotStatus.PowerLow)
                        {
                            var voice = voiceLibrary.GetRandomPowerLow();
                            if (voice)
                            {
                                VoiceQueue.Play(new VoiceLine(voice, "voicePowerLow", 1));
                            }
                            else
                                Log.Error("Voice for PowerLow not found");
                        }
                        break;
                    case AutopilotStatus.PowerCritical:
                    case AutopilotStatus.PowerDead:
                        if (statusChange.PreviousStatus < AutopilotStatus.PowerCritical)
                        {
                            var voice = voiceLibrary.GetRandomPowerCritical();
                            if (voice)
                            {
                                VoiceQueue.Play(new VoiceLine(voice, "voicePowerCritical", 2));
                            }
                            else
                                Log.Error("Voice for PowerCritical not found");
                        }
                        break;
                }
                Log.Write($"Received autopilot status change: {statusChange.PreviousStatus} -> {statusChange.NewStatus}");
            }
        }

        internal bool IsDockedBySavegame(PrefabIdentifier? prefabIdentifier)
        {
            if (DockedSubPrefabIds == null)
            {
                Log.Write($"No docked vehicles restored from last load operation");
                return false;
            }
            if (prefabIdentifier == null)
            {
                Log.Error($"Candidate has no PrefabIdentifier");
                return false;
            }
            if (!DockedSubPrefabIds.Contains(prefabIdentifier.Id))
            {
                Log.Error($"Prefab ID {prefabIdentifier.Id} is not declared in list of docked prefab IDs");
                return false;
            }
            return true;
        }


        //public override List<VehicleHatchStruct> Hatches
        //{
        //    get
        //    {
        //        var hatches = transform.Find("Hatches");
        //        if (!hatches)
        //        {
        //            Log.Error("Hatches not found");
        //            return new List<VehicleHatchStruct>();
        //        }
        //        var rs = new List<VehicleHatchStruct>();
        //        foreach (Transform hatch in hatches)
        //        {
        //            var exit = hatch.Find("Exit");
        //            var entry = hatch.Find("Entry");
        //            if (!exit || !entry)
        //            {
        //                Log.Error("Hatch children not found of " + hatch);
        //                continue;
        //            }
        //            rs.Add(new VehicleHatchStruct
        //            {
        //                Hatch = hatch.gameObject,
        //                ExitLocation = exit,
        //                SurfaceExitLocation = exit,
        //                EntryLocation = entry
        //            });
        //        }
        //        Log.Write($"Returning {rs.Count} hatch(es)");

        //        return rs;
        //    }
        //}

        //public override GameObject VehicleModel => staticModel;

        //public override GameObject CollisionModel => transform.Find("CollisionModel").gameObject;
        //public override GameObject BoundingBox => transform.Find("EntireBoundingBox").gameObject;
        //public override PilotingStyle pilotingStyle => PilotingStyle.Other;

        //public override List<VehicleStorage> ModularStorages
        //{
        //    get
        //    {
        //        var root = transform.Find("StorageRoot").gameObject;
        //        var rs = new List<VehicleStorage>();
        //        if (root == null)
        //            return rs;
        //        for (int i = 0; i < 8; i++)
        //        {
        //            var name = $"Storage{i}";
        //            var storageTransform = root.transform.Find(name);
        //            if (storageTransform == null)
        //            {
        //                storageTransform = new GameObject(name).transform;
        //                storageTransform.parent = root.transform;
        //                storageTransform.localPosition = M.V3(i);
        //                Log.Write($"Creating new storage transform {storageTransform} in {root} @{storageTransform.localPosition} => {storageTransform.position}");
        //            }
        //            rs.Add(new VehicleStorage
        //            {
        //                Container = storageTransform.gameObject,
        //                Height = 2,
        //                Width = 2
        //            });
        //        }
        //        return rs;

        //    }
        //}
        //public override List<GameObject> WaterClipProxies
        //{
        //    get
        //    {
        //        return new List<GameObject>();
        //    }
        //}

        //public override List<VehicleUpgrades> Upgrades
        //{
        //    get
        //    {
        //        var rs = new List<VehicleUpgrades>();
        //        var ui = transform.Find("UpgradesInterface");
        //        var plugs = transform.Find("Module Plugs");

        //        var plugProxies = new List<Transform>();
        //        if (plugs != null)
        //        {
        //            for (int i = 0; i < plugs.childCount; i++)
        //            {
        //                var plug = plugs.GetChild(i);
        //                var position = plug.Find("Module Position");
        //                if (position != null)
        //                    plugProxies.Add(position);
        //                else
        //                    Log.Write($"Plug {plug.name} does not have a 'Module Position' child");
        //            }
        //        }
        //        else
        //            Log.Write($"Plugs not found");

        //        Log.Write($"Determined {plugProxies.Count} plug(s)");

        //        if (ui != null)
        //        {
        //            rs.Add(new VehicleUpgrades
        //            {
        //                Interface = ui.gameObject,
        //                Flap = ui.gameObject,
        //                ModuleProxies = plugProxies
        //            });
        //        }
        //        else
        //            Log.Write($"Upgrades interface not found");
        //        return rs;

        //    }

        //}

        //public override List<VehicleBattery> Batteries
        //{
        //    get
        //    {
        //        var rs = new List<VehicleBattery>();


        //        var batteries = transform.Find("Batteries");

        //        if (batteries != null)
        //        {
        //            for (int i = 0; i < batteries.childCount; i++)
        //            {
        //                var b = batteries.GetChild(i);
        //                if (b != null)
        //                {
        //                    rs.Add(new VehicleBattery
        //                    {
        //                        BatterySlot = b.gameObject,
        //                        BatteryProxy = b
        //                    });
        //                }
        //            }
        //        }
        //        else
        //            Log.Write($"Unable to locate 'Batteries' child");
        //        return rs;
        //    }

        //}


        //public override VFEngine VFEngine { get; set; }

        //private List<VehicleFloodLight> headLights = new List<VehicleFloodLight>();

        //public override List<VehicleFloodLight> HeadLights
        //{
        //    get
        //    {
        //        //Log.Write($"Get HeadLights");
        //        //if (headLights is null)
        //        //{

        //        //    headLights = new List<VehicleFloodLight>();
        //        //    try
        //        //    {
        //        //        var hl = transform.GetComponentsInChildren<Light>();
        //        //        Log.Write($"processing {hl.Length} headlight(s)");


        //        //        if (hl.Length > 0)
        //        //        {
        //        //            foreach (var light in hl)
        //        //                if (light.type == LightType.Spot && light.transform.name != "Center Light")
        //        //                {
        //        //                    var go = new GameObject($"Light Dummy for {light.name}");
        //        //                    go.transform.parent = light.transform.parent;
        //        //                    go.transform.localPosition = light.transform.localPosition;
        //        //                    go.transform.localRotation = light.transform.localRotation;
        //        //                    Log.Write($"Reparenting light {light} to {go}");
        //        //                    light.transform.parent = go.transform;
        //        //                    light.transform.localPosition = Vector3.zero;
        //        //                    light.transform.localRotation = Quaternion.identity;
        //        //                    light.transform.name = light.name = "VolumetricLight";

        //        //                    headLights.Add(new VehicleFloodLight
        //        //                    {
        //        //                        Angle = light.spotAngle,
        //        //                        Color = light.color,
        //        //                        Intensity = light.intensity,
        //        //                        Light = go,
        //        //                        Range = light.range
        //        //                    });
        //        //                }
        //        //        }
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        Log.Write("HeadLights", ex);
        //        //    }
        //        //    Log.Write($"Returning {headLights.Count} headlight(s)");
        //        //}
        //        return headLights;

        //    }

        //}

        //public override List<VehiclePilotSeat> PilotSeats
        //{
        //    get
        //    {
        //        var rs = new List<VehiclePilotSeat>();
        //        var cockpit = transform.Find("Cockpit");
        //        if (!cockpit)
        //        {
        //            Log.Write("Cockpit not found");
        //            return rs;
        //        }
        //        var entries = transform.Find("Interior/Entries");
        //        foreach (var entry in entries.GetChildren())
        //        {
        //            var cockpitExit = entry.Find($"Exit");
        //            if (!cockpitExit)
        //            {
        //                Log.Write($"Cockpit exit not found for {entry.NiceName()}");
        //                continue;
        //            }
        //            rs.Add(new VehiclePilotSeat
        //            {
        //                Seat = entry.gameObject,
        //                SitLocation = cockpit.gameObject,
        //                ExitLocation = cockpitExit,
        //                LeftHandLocation = cockpit,
        //                RightHandLocation = cockpit,
        //            });
        //        }
        //        return rs;
        //    }
        //}


        //public override List<GameObject> TetherSources
        //{
        //    get
        //    {
        //        if (tetherSources is null)
        //        {
        //            tetherSources = new List<GameObject>();
        //            var tether = transform.Find("Tether");
        //            if (!tether)
        //            {

        //                Log.Error("Tether not found. No tethers will be defined");

        //            }
        //            else
        //            {
        //                foreach (Transform trans in tether)
        //                {
        //                    var t = trans.GetComponent<SphereCollider>();
        //                    if (!t)
        //                    {
        //                        Log.Error($"Tether {trans} does not hace a sphere collider");
        //                        continue;
        //                    }
        //                    t.radius = t.transform.localScale.x;
        //                    t.transform.localScale = Vector3.one;
        //                    tetherSources.Add(t.gameObject);
        //                }
        //                Log.Write($"Recorded {tetherSources.Count} tether source(s)");
        //            }

        //        }
        //        return tetherSources;
        //    }
        //}

    }


}
