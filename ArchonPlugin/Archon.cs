using Assets.Behavior.Components.Motion;
using AVS;
using AVS.Assets;
using AVS.Composition;
using AVS.Configuration;
using AVS.Interfaces;
using AVS.Log;
using AVS.SaveLoad;
using AVS.Util;
using AVS.VehicleBuilding;
using AVS.VehicleComponents;
using AVS.VehicleTypes;
using FMOD.Studio;
using FMODUnity;
using Subnautica_Archon.Adapters;
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



    public class Archon : Submarine, IPowerListener, IAutopilotEventListener
    {
        public static GameObject? staticModel;
        private ArchonControl? control;
        public ArchonControl Control => control.OrThrow(
            () =>
            new InvalidOperationException($"Trying to access Control before Awake()"));




        public static readonly VehicleColor defaultBaseColor = new VehicleColor(new Color(0xDE, 0xDE, 0xDE) / 255f);
        public static readonly VehicleColor defaultStripeColor = new VehicleColor(new Color(0x3F, 0x4C, 0x7A) / 255f);

        //private List<GameObject> tetherSources;
        //tracks true if vehicle death was ever determined. Can't enter in this state
        private bool wasDead;
        /// <summary>
        /// True if this component has been destroyed and is no longer usable.
        /// </summary>
        public bool destroyed;
        //private MyLogger Log { get; }
        private MassDrive? engine;
        public MassDrive Engine => engine.OrThrow(
            () => new InvalidOperationException($"Trying to access Engine before Awake()"));
        private EnergyInterface? energyInterface;
        private readonly int[] moduleCounts = new int[Enum.GetValues(typeof(ArchonModule)).Length];

        private bool? clippingWater;
        private bool isInCriticalRecovery;
        private Dictionary<string, VoiceLibrary> VoiceLibraries { get; } = new Dictionary<string, VoiceLibrary>();


        public VoiceLibrary? GetVoiceLibrary()
        {
            if (ArchonModController.PluginConfig.voice == Voice.Off)
                return null;
            VoiceLibraries.TryGetValue(ArchonModController.PluginConfig.voice.ToString(), out var voiceLibrary);
            return voiceLibrary;
        }
        public Vector3? TeleportationTargetA { get; set; }
        public Vector3? TeleportationOrientationA { get; set; }


        public Archon() : base(new VehicleConfiguration(
            unlockedSprite: ArchonModController.StaticImages.ArchonCraftingSprite,
            maxHealth: 50000,
            crushDamage: 50000 / (60 * 2),    //damage so that total failure is achieved after 2 minutes at crush depth
            mass: 20000,
            numModules: 8,
            craftingSprite: ArchonModController.StaticImages.ArchonCraftingSprite,
            pingSprite: ArchonModController.StaticImages.ArchonPingSprite,
            saveFileSprite: ArchonModController.StaticImages.ArchonPingSprite,
            moduleBackgroundImage: ArchonModController.StaticImages.ModulesBackground,
            description: Language.main.Get("General.Description"),
            encyclopediaEntry: Language.main.Get("General.Encyclopedia"),
            canLeviathanGrab: false,
            canMoonpoolDock: false,
            pilotingStyle: PilotingStyle.Other,
            canEnterHelmWithoutPower: true,
            materialAdaptConfig: new MaterialAdaptConfig(),
            recipe: NewRecipe
                .Add(TechType.PowerCell, 3)
                .Add(TechType.AdvancedWiringKit, 2)
                .Add(TechType.Diamond, 6)
                .Add(TechType.PlasteelIngot, 5)
                .Add(TechType.Aerogel, 3)
                .Add(TechType.Lubricant, 1)
                .Done(),
            getVoiceSoundVolume: () => ArchonModController.PluginConfig.voiceVolumePercent / 100f
            * 0.5f

            ,
            getVoiceSubtitlesEnabled: () => ArchonModController.PluginConfig.showVoiceSubtitles
        ))
        {
            //can't use log here, instance not assigned yet
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
                    Persistable.Property("TeleportationTargetA",
                        () => TeleportationTargetA,
                        c => TeleportationTargetA = c
                        ),
                    Persistable.Property("TeleportationOrientation1",
                        () => TeleportationOrientationA,
                        c => TeleportationOrientationA = c
                        ),
                    Persistable.Property("IsInCriticalRecovery",
                        () => isInCriticalRecovery,
                        b => isInCriticalRecovery = b
                        ),
                    Persistable.Property("FreeCameraInCockpit",
                        () => Control.freeCameraInCockpit,
                        b => Control.freeCameraInCockpit = b
                        ),
                    Persistable.Property("FreeCameraInExternalCamera",
                        () => Control.freeCameraInExternalCamera,
                        b => Control.freeCameraInExternalCamera = b
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
                            using var log = NewModLog();
                            log.Write($"Docked sub prefabs restored from file: {string.Join(", ", list)}");
                        }
                        )
                ));
            base.CreateDataBlocks(addBlock);
        }

        public IEnumerable<QuickSlot> QuickSlots
        {
            get
            {
                for (int i = 0; i < slotIDs.Length; i++)
                    yield return new QuickSlot(i, slotIDs[i]);
            }
        }

        public static Pickupable? AutoAddEmergencyTeleport { get; set; }

        public override void OnFinishedLoading()
        {
            using var log = NewModLog();

            log.Write($"Comparing colors {BaseColor} and {StripeColor}");
            if (BaseColor == VehicleColor.Default && StripeColor == VehicleColor.Default)
            {
                log.Write($"Resetting default color {VehicleName}");
                SetBaseColor(defaultBaseColor);
                SetStripeColor(defaultStripeColor);
            }


            if (AutoAddEmergencyTeleport != null)
            {
                var cnt = modules.GetCount(EmergencyTeleportationModule.Type);
                if (cnt == 0)
                {
                    log.Write($"onAwakeSlot is set to {AutoAddEmergencyTeleport.NiceName()}. Instantiating");
                    var instance = Instantiate(AutoAddEmergencyTeleport.gameObject, modulesRoot.transform).GetComponent<Pickupable>();
                    instance.transform.SetParent(modulesRoot.transform, false);
                    instance.gameObject.SetActive(false);
                    //var instance = autoAddEmergencyTeleport;
                    log.Write($"Slotting instance {instance.NiceName()}");
                    InventoryItem thisItem = new InventoryItem(instance);
                    bool success = false;
                    foreach (var slot in slotIDs.Reverse())
                    {
                        if (modules.AddItem(slot, thisItem, true))
                        {
                            log.Write($"Slotted in {slot}");
                            success = true;
                            break;
                        }
                    }
                    if (!success)
                    {
                        log.Error($"Failed to slot {instance.NiceName()} anywhere");
                        Destroy(instance.gameObject);
                    }
                }
                else
                    log.Write($"onAwakeSlot is set to {AutoAddEmergencyTeleport.NiceName()}. But Emergency Teleportation Module ({EmergencyTeleportationModule.Type.AsString()}) already exists ({cnt} instances). Not instantiating");
            }
            else
                log.Write($"onAwakeSlot is not set");



            Control.RedetectDocked();

            base.OnFinishedLoading();

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


        public static GameObject GetAssets(ArchonModController amc)
        {
            using var log = SmartLog.For(amc);

            try
            {
                var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (modPath.IsNull())
                    throw new IOException("Unable to get mod path");
                string bundlePath;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    bundlePath = Path.Combine(modPath, "archon.osx");
                else
                    bundlePath = Path.Combine(modPath, "archon");
                log.Write($"Trying to load asset bundle from '{bundlePath}'");
                if (!File.Exists(bundlePath))
                    log.Write("This file does not appear to exist");
                var bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle != null)
                {
                    var assets = bundle.LoadAllAssets();
                    foreach (var obj in assets)
                    {
                        log.Write("Scanning object: " + obj.NiceName());
                        if (obj.name == "Archon")
                        {
                            staticModel = (GameObject)obj;
                        }
                    }
                    if (staticModel.IsNull())
                        log.Write("Model not found among: " + string.Join(", ", Helper.Names(assets)));
                }
                else
                    log.Write("Unable to load bundle from path");
            }
            catch (Exception ex)
            {
                log.Error(nameof(GetAssets), ex);
            }
            return staticModel.OrThrow(() => throw new IOException("Unable to load Archon model. Please check your installation"));
        }

        void OnDestroy()
        {
            Util.Log.Write($"{VehicleName} " + nameof(OnDestroy));
            destroyed = true;
        }


        private bool isInitialized;
        private bool hadUnpausedFrame;

        public override void SubConstructionComplete()
        {
            base.SubConstructionComplete();
            SetBaseColor(defaultBaseColor);
            SetStripeColor(defaultStripeColor);
        }

        public override void Awake()
        {
            using var log = NewModLog();

            worldForces.aboveWaterDrag = worldForces.underwaterDrag = 0;


            BayControl.OnDockingFailedFull = (_, _) =>
            {
                Log.Write($"full");
                AVS.Logger.PDANote("Cannot dock: Hangar is full", 3f);
            };

            BayControl.OnDockingFailedTooLarge = (_, _) =>
            {
                Log.Write($"too large");
                AVS.Logger.PDANote("Cannot dock: Your vehicle is too large", 3f);
            };

            //onToggle += OnQuickbarToggle;



            control = GetComponent<ArchonControl>();
            control.interiorLightScale = 0.75f;

            //var loadSave = gameObject.GetComponent<LoadSaveComponent>();
            //if (!loadSave)
            //    loadSave = gameObject.AddComponent<LoadSaveComponent>();
            //loadSave.control = control;

            //Destroy(modulesRoot);

            //modulesRoot = control.hangarRoot.gameObject.AddComponent<ChildObjectIdentifier>();

            var interior = transform.Find("Interior");
            if (interior)
            {
                var reactorTransform = interior.Find("Bioreactor");
                if (reactorTransform)
                {
                    reactor = reactorTransform.gameObject.EnsureComponent<MaterialReactor>();
                    reactor.Initialize(this, 6, 6, AVS.Localization.Text.Translated("Component.Bioreactor"), 0, MaterialReactor.GetBioReactorData());
                    reactor.canViewWhitelist = false;
                }
                else
                    log.Error("Unable to find Biofuel Storage child");
            }
            else
                log.Error("Unable to find Interior child");



            var mapWorld = transform.Find("Interior/Map Table/Display/World");
            if (mapWorld != null)
            {
                log.Write($"Found map world {mapWorld.NiceName()}. Trying to build mini-world");
                try
                {
                    SpawnMiniWorld(mapWorld, Control.mapHologramMaterial, 500);
                    log.Write($"Map instantiated");
                }
                catch (Exception ex)
                {
                    log.Error($"Error instantiating map", ex);
                }
            }
            else
            {
                log.Write($"Interior/Map Table/Display/World not found");
            }


            base.Awake();

            if (ArchonModController.PluginConfig.defaultToFirstPerson)
                Control.SetCameraFirstPerson(true);

        }

        //private void OnQuickbarToggle(int slotID, bool state)
        //{
        //    if (state == true)
        //    {
        //        var slotId = new QuickSlot(slotID, slotIDs[slotID]);
        //        var item = modules.GetItemInSlot(slotId.ID)?.item;
        //        if (item.IsNull())
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
            using var log = NewModLog();
            if (exitLimitsSuspended)
                base.DeselectSlots();
            else
            {
                if (!AbortAutoLeveling())
                {
                    log.Write("Starting new exit loop");
                    autoLevelRoutine = Owner.StartModCoroutine(
                        nameof(Archon) + '.' + nameof(AutoLevelThenExit),
                        AutoLevelThenExit);
                }
            }
        }

        public bool AbortAutoLeveling()
        {
            if (autoLevelRoutine != null)
            {
                using var log = NewModLog();

                log.Write("Exit loop in progress. Aborting");
                StopCoroutine(autoLevelRoutine);
                autoLevelRoutine = null;
                Logger.PDANote($"Auto-leveling aborted");
                Control.doAutoLevel = false;
                log.Write("Aborted. Control restored");
                return true;
            }
            return false;
        }

        private IEnumerator AutoLevelThenExit(SmartLog log)
        {
            log.Write(nameof(AutoLevelThenExit));
            var voiceLibrary = GetVoiceLibrary();
            if (Control.IsLevel)
            {
                log.Write("Archon is level. Exiting now");


                base.DeselectSlots();
                autoLevelRoutine = null;
                yield break;
            }

            log.Write("Archon is not level. Leveling out");
            var voice = voiceLibrary?.GetRandomAutoLeveling();
            VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Leveling.Starting"));

            Control.doAutoLevel = true;
            //var timewindow = TimeSpan.FromSeconds(5);
            //var deadline = DateTime.Now + timewindow;
            float timewindow = 5;
            var remaining = timewindow;
            while (Control is { doAutoLevel: true, IsLevel: false } && remaining > 0)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }
            log.Write("Archon is level or deadline has passed");
            autoLevelRoutine = null;
            if (Control.doAutoLevel)
            {
                log.Write("Archon leveling has not been aborted");
                Control.doAutoLevel = false;
                if (Control.IsLevel)
                {
                    log.Write("Archon is level. Exiting");

                    base.DeselectSlots();
                }
                else
                {
                    log.Write("Archon is not level. Not exiting");

                    voice = voiceLibrary?.GetRandomAutoLevelingHasFailed();
                    VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Leveling.Failed"));
                }
            }
        }


        private void LazyInit()
        {
            if (!isInitialized)
            {
                using var log = NewModLog();
                log.Write($"LocalInit() first time");
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

                    //if (rotateCamera.IsNull())
                    //    EchLog.Write($"Rotate camera not found");
                    //else
                    //    EchLog.Write($"Found camera rotate {rotateCamera.name}");
                    Control.RedetectDocked();
                    if (control != null)
                    {
                        log.Write("Found control");
                    }
                    else
                    {
                        if (transform.IsNull())
                            log.Write($"Do not have a transform");
                        else
                        {
                            log.Write($"This is {transform.name}");
                            log.Write("This has components: " + Helper.NamesS(Helper.AllComponents(transform)));
                            log.Write("This has children: " + Helper.NamesS(Helper.Children(transform)));
                        }
                    }
                    log.Write($"LocalInit() done");

                }
                catch (Exception e)
                {
                    log.Error("LocalInit()", e);
                }

            }
        }


        public override void SetBaseColor(VehicleColor color)
        {
            using var log = NewModLog();
            log.Write($"Updating sub base color to {color}");
            base.SetBaseColor(color);

            var listeners = GetComponentsInChildren<IColorListener>();
            foreach (var listener in listeners)
                listener.SetColors(BaseColor.RGB, StripeColor.RGB);

        }

        public override void SetStripeColor(VehicleColor color)
        {
            using var log = NewModLog();
            log.Write($"Updating sub stripe color to {color}");
            base.SetStripeColor(color);

            var listeners = GetComponentsInChildren<IColorListener>();
            foreach (var listener in listeners)
                listener.SetColors(BaseColor.RGB, StripeColor.RGB);
        }


        public override void Start()
        {
            using var log = NewModLog();
            try
            {


                LazyInit();

                base.Start();
            }
            catch (Exception ex)
            {
                log.Error(nameof(Start), ex);
            }
        }



        protected override void OnPrePlayerEntry()
        {
            using var log = NewModLog();

            Control.Enter(Helper.GetPlayerReference(), skipOrientation: exitLimitsSuspended || !hadUnpausedFrame);
            HudPingInstance.SetHudIcon(log, false);

            base.OnPrePlayerEntry();
        }

        protected override void OnPlayerExit()
        {
            using var log = NewModLog();
            base.OnPlayerExit();
            HudPingInstance.SetHudIcon(log, true);
            Control.Exit();

        }



        protected override void OnPreBeginHelmControl(Helm helm)
        {
            using var log = NewModLog();
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
                log.Error(nameof(OnPreBeginHelmControl), ex);
            }
        }

        protected override void OnBeginHelmControl(Helm helm)
        {
            using var log = NewModLog();
            try
            {
                base.OnBeginHelmControl(helm);
                LazyInit();

                Control.Control(Helper.GetPlayerReference());
            }
            catch (Exception ex)
            {
                log.Error(nameof(OnBeginHelmControl), ex);
            }

        }

        protected override void OnPreEndHelmControl()
        {
            using var log = NewModLog();
            try
            {

                LazyInit();
                Control.ExitControl(Helper.GetPlayerReference(), skipOrientation: exitLimitsSuspended);
            }
            catch (Exception ex)
            {
                log.Error(nameof(OnPreEndHelmControl), ex);
            }
        }

        protected override void OnEndHelmControl()
        {
            using var log = NewModLog();
            try
            {
                base.OnEndHelmControl();

                if (Player.main.sitting)
                {
                    log.Error($"Player is still sitting after control exit");
                    Player.main.sitting = false;
                    Player.main.playerController.ForceControllerSize();
                }
                else
                    log.Write($"Sitting not detected");

                Player.main.transform.LookAt(transform.position);

            }
            catch (Exception ex)
            {
                log.Error(nameof(OnEndHelmControl), ex);
            }
        }

        private bool fixedUpdateError;
        private bool wasAboveWater;

        private PARAMETER_ID verticalVelocitySoundIndex = FMODUWE.invalidParameterId;
        private void PlaySplashSound()
        {
            EventInstance ev = FMODUWE.GetEvent(splashSound);
            ev.set3DAttributes(transform.position.To3DAttributes());
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
            using var log = NewModLog();
            var clipProxyParent = transform.Find("WaterClipProxy");
            var seamoth = SeamothHelper.Seamoth;
            if (seamoth.IsNull())
            {
                log.Write("Seamoth prefab not found. Can't adjust clip proxies right now");
                return;
            }
            if (clipProxyParent && seamoth != null)
            {
                bool isGood = false;
                var meta = transform.GetComponentInChildren<DistanceFieldMeta>();
                if (meta != null && meta.distanceField != null)
                {
                    isGood = true;
                    if (enable)
                        WaterClipUtil.BindProxy(Owner, clipProxyParent.gameObject, meta.distanceField, meta.localBounds);
                    else
                        WaterClipUtil.UnbindProxy(Owner, clipProxyParent.gameObject);
                    log.Write($"Flushing children...");
                    clipProxyParent.DestroyChildren();
                    log.Write($"All done");
                }
                else
                    log.Error($"DistanceFieldMeta not found on {transform.NiceName()} or it has no texture. Can't bind distance field to clip proxy parent {clipProxyParent.NiceName()}");

                if (!isGood)
                {
                    log.Error($"Unable to bind distance field to clip proxy parent {clipProxyParent.name}. Using default clip proxies instead");
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
                            //"""Apply the SeaMoth's clip material. No idea what shader it uses or what settings it actually has, so this is an easier option. Reuse the game's assets.""" -Lee23
                            waterClip.clipMaterial = seamothWCP.clipMaterial;
                            //"""You need to do this. By default, the layer is 0. This makes it displace everything in the default rendering layer. We only want to displace water.""" -Lee23
                            waterClip.gameObject.layer = seamothWCP.gameObject.layer;
                        }
                    }
                }
                clippingWater = enable;
                log.Write($"Water-clip proxies adapted ({enable} ({ClipWaterS}))");

            }
            else
                log.Write("Clip proxies or seamoth not found. Can't adjust right now");
        }

        public bool ClipWater => Control is { CameraIsInVehicle: true, BoardedByHeadless: false };
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

        public bool WillRechargingDocked { get; private set; }
        public bool WillRepairDocked { get; private set; }

        private void ProcessEnergyRecharge()
        {

            WillRechargingDocked = false;



            if (energyInterface != null)
            {
                energyInterface.GetValues(out var myCharge, out var myCapacity);

                if (myCharge > myCapacity * 0.05f && Time.deltaTime > 0)
                {
                    WillRechargingDocked = true;
                    foreach (var docked in Control.bayControl.Docked)
                    {
                        if (docked is DockableVehicle v)
                        {
                            var dockedEnergyInterface = v.Vehicle.GetComponent<EnergyInterface>();
                            if (dockedEnergyInterface != null)
                            {
                                dockedEnergyInterface.GetValues(out var dockedCharge, out var dockedCapacity);
                                if (dockedCharge < dockedCapacity)
                                {
                                    float recharge = Mathf.Min(
                                        0.005f * Time.deltaTime * dockedCapacity,
                                        dockedCapacity - dockedCharge,
                                        myCharge);
                                    energyInterface.ConsumeEnergy(recharge);
                                    dockedEnergyInterface.AddEnergy(recharge);
                                }
                            }
                        }
                    }
                }

                //                var batteryMk = GetBatteryMark();

                //float level = 1;

                //float recharge =
                //      0.4f  //max 1.6 per second
                //    * level;

                //energyInterface.ModifyCharge(
                //    Time.deltaTime
                //    * recharge
                //    );
                Control.currentEnergy = myCharge;
                Control.maxEnergy = myCapacity;


            }
        }

        private void ProcessRegeneration()
        {
            Control.isHealing = false;

            var delta = Time.deltaTime;

            WillRepairDocked = false;
            if (liveMixin != null)
            {

                if (delta > 0)
                {

                    var criticalHealingLimit = liveMixin.maxHealth * 0.1f;
                    var critical = liveMixin.health < liveMixin.maxHealth * 0.05f;
                    if (critical && !isInCriticalRecovery)
                    {
                        AudioClip? voice = GetVoiceLibrary()?.GetRandomEmergencyRepairEnabled();
                        VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.CriticalHealth.RepairEnabled", 1));


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
                            50
                            * delta
                            //* MainPatcher.PluginConfig.selfHealingSpeed / 100   //if slower, cost less
                            * effective //if clamped, cost less
                            ;
                        PowerManager.TrySpendEnergy(energyDemand);

                        liveMixin.AddHealth(clamped);
                        Control.isHealing = true;
                        liveMixin.invincible = true;
                    }
                    else if (isInCriticalRecovery)
                    {
                        AudioClip? voice = GetVoiceLibrary()?.GetRandomEmergencyRepairConcluded();
                        VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.CriticalHealth.RepairDone", 1));

                        Log.Warn($"Emergency healing concluded switching off");
                        isInCriticalRecovery = false;
                        liveMixin.invincible = false;
                    }
                    else if (!Control.batteryDead)
                    {
                        float level = RepairModule.GetRelativeSelfRepair(RepairModule.GetFrom(this));
                        if (level > 0)
                        {
                            WillRepairDocked = true;
                            foreach (var docked in Control.bayControl.Docked)
                            {
                                if (docked is DockableVehicle v)
                                {
                                    var dockedLive = v.Vehicle.liveMixin;
                                    if (dockedLive)
                                    {
                                        if (dockedLive.health < dockedLive.maxHealth)
                                        {
                                            var healing = dockedLive.maxHealth
                                                          * delta
                                                          * level;
                                            var clamped = Mathf.Min(healing, dockedLive.maxHealth - dockedLive.health);
                                            var effective = clamped / healing;
                                            float energyDemand =
                                                    10 * dockedLive.maxHealth / liveMixin.maxHealth //less health => less energy
                                                    * delta
                                                    //* MainPatcher.PluginConfig.selfHealingSpeed / 100   //if slower, cost less
                                                    * effective //if clamped, cost less
                                                ;
                                            PowerManager.TrySpendEnergy(energyDemand);
                                            var actuallyHealed = clamped;
                                            dockedLive.AddHealth(actuallyHealed);
                                        }
                                    }
                                }

                            }



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
                }
                Control.maxHealth = liveMixin.maxHealth;
                Control.currentHealth = liveMixin.health;

            }
        }

        private void ForwardControlAxes()
        {
            if (Control.batteryDead || Control.powerOff || engine.IsNull())
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
            if (engine.IsNull())
            {
                return;
            }
            engine.overdriveActive = 0;
            engine.doNotAccelerate = Control.doAutoLevel || Control.batteryDead;
            engine.freeCamera = Control.UseFreeCamera;
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
                && !Character.IsAnyMenuOpen
                )
            {
                if (GameInput.GetButtonDown(GameInput.Button.RightHand))
                {
                    SetLights(!Control.floodLights);
                }
            }

        }

        private void SetLights(bool on)
        {
            if (Control.floodLights == on)
                return;
            if (on && (Control.batteryDead || Control.powerOff))
            {
                Log.Warn($"Battery dead or ship powered off. Cannot turn lights on");
                return;
            }
            Control.floodLights = on;
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

        ///// <summary>
        ///// Redetects proximity to the ocean surface and forwards the state to control
        ///// </summary>
        //private void RepositionCamera()
        //{
        //    Control.UpdateLowCamera(Ocean.GetOceanLevel());
        //}

        private bool HasModule(ArchonModule module)
            => moduleCounts[(int)module] > 0;


        public ArchonModule HighestModuleType(params ArchonModule[] m)
        {
            for (int i = m.Length - 1; i >= 0; i--)
                if (HasModule(m[i]))
                    return m[i];
            return ArchonModule.None;
        }

        //private MaterialFixer MaterialFixer;

        // ReSharper disable twice NotAccessedField.Local
        private Color nonBlackBaseColor;
        private Color nonBlackStripeColor;

        //public override void OnVehicleDocked(Vehicle vehicle, Vector3 exitLocation)
        //{
        //    base.OnVehicleDocked(vehicle, exitLocation);
        //    SetBaseColor(Vector3.zero, nonBlackBaseColor);
        //    SetStripeColor(Vector3.zero, nonBlackStripeColor);
        //}


        private MenuTracker MenuTracker { get; } = new();


        public override void Update()
        {
            try
            {
                LazyInit();

                if (reactor != null)
                    Control.reactorIsCharging = reactor.isGeneratingEnergy;

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
                switch (ArchonModController.PluginConfig.interiorLights)
                {
                    case InteriorLights.Full:
                        Control.minimumInteriorLightPriority = 0;
                        break;
                    case InteriorLights.Reduced:
                        Control.minimumInteriorLightPriority = 1;
                        break;
                    case InteriorLights.Minimal:
                        Control.minimumInteriorLightPriority = 2;
                        break;

                }

                Control.flipFreeHorizontalRotationInReverse = ArchonModController.PluginConfig.flipFreeHorizontalRotationInReverse;
                Control.flipFreeVerticalRotationInReverse = ArchonModController.PluginConfig.flipFreeVerticalRotationInReverse;

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
                    //if (Control.IsBoardedButNotControlled)
                    {
                        //OutOfBoundsWarp
                        //EcoTarget
                        //SDFCutout
                        Log.Write("Debug action");
                        //Log.Write($"@{transform.position}");

                        //Log.Write($"Modules now: ");
                        //foreach (var slot in slotIDs)
                        //{
                        //    Log.Write($"Slot {slot} has item [{modules.GetItemInSlot(slot)?.item.NiceName()}]");
                        //}

                        //var colliders = Physics.OverlapSphere(Player.main.transform.position, 1000, 0x1fffffff, QueryTriggerInteraction.Ignore);
                        //var roots = colliders
                        //    .Where(x => x.attachedRigidbody != null && x.attachedRigidbody.gameObject != null)
                        //    .Select(x => x.attachedRigidbody.gameObject.GetComponentInChildren<SubRoot>())
                        //    .Where(x => x != null)
                        //    .ToHashSet();


                        //foreach (var root in roots)
                        //{
                        //    var prefabId = root.GetComponentInChildren<PrefabIdentifier>();
                        //    string name = prefabId.SafeGet(x => x.Id, root.GetInstanceID().ToString());
                        //    Log.Write("Analyzing " + root.NiceName() + " -> " + name);
                        //    new AVS.Util.HierarchyAnalyzer().LogToJson(root.transform, $"C:\\temp\\v3_{name}.json");
                        //}
                        //TryFixLostBuildFocus();
                        //Control.interiorColliders.gameObject.SetActive(false);
                        //StartCoroutine(ReenableColliders());
                    }
                }




                if (!liveMixin.IsAlive() || wasDead)
                {
                    Log.Warn($"Vehicle reported as dead. Reviving. Setting invincible");
                    UnscuttleVehicle();
                    wasDead = false;
                    liveMixin.health = liveMixin.maxHealth * 0.01f;
                    liveMixin.invincible = true;    //archon is immortal
                }

                //ArchonControl.targetArrows = MainPatcher.PluginConfig.targetArrows;


                Vector2 lookDelta = GameInput.GetLookDelta();

                if (Character.IsAnyMenuOpen)
                    Control.lookRightAxis = Control.lookUpAxis = 0;
                else
                {
                    Control.lookRightAxis = lookDelta.x * 1e-3f * ArchonModController.PluginConfig.lookSensitivity;
                    Control.lookUpAxis = lookDelta.y * 1e-3f * ArchonModController.PluginConfig.lookSensitivity;
                }

                Control.floodLightShadows = ArchonModController.PluginConfig.floodLightShadows;
                Control.engineSoundVolume = ArchonModController.PluginConfig.engineSoundVolume * 0.01f;

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
                        ((Input.GetKey(ArchonModController.PluginConfig.btnAltZoomOut) ? 1f : 0f)
                        - (Input.GetKey(ArchonModController.PluginConfig.btnAltZoomIn) ? 1f : 0f)) * 0.02f
                        ;
                }

                if (Control.IsBeingControlled
                    && Input.GetKeyDown(ArchonModController.PluginConfig.toggleFreeCamera)
                    && engine != null)
                {
                    Control.ToggleCurrentFreeCamera();
                    engine.freeCamera = Control.UseFreeCamera;
                }
                if (Input.GetKeyDown(ArchonModController.PluginConfig.btnChangeExternalCameraHeight))
                {
                    Control.positionCameraBelowSub = !Control.positionCameraBelowSub;
                }


                ProcessEngine();


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
            if (!Control.batteryDead)
                SetLights(true);
        }

        public void OnPowerDown()
        {
            Control.powerOff = true;
            SetLights(false);
        }

        public void OnBatteryDead()
        {
            Control.batteryDead = true;
            SetLights(false);
        }

        public void OnBatteryRevive()
        {
            Control.batteryDead = false;
            if (!Control.powerOff)
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

        internal int GetExtraDockingCapacity()
        {
            return moduleCounts[(int)ArchonModule.DockingModuleMk1];
        }

        internal int GetTotalDockingCapacity()
        {
            return 2 + GetExtraDockingCapacity();
        }

        internal int GetTotalDockingCapacityWithOneLess(ArchonModule moduleType)
        {
            if (moduleCounts[(int)moduleType] == 0)
                return GetTotalDockingCapacity();
            moduleCounts[(int)moduleType]--;
            int total = GetTotalDockingCapacity();
            moduleCounts[(int)moduleType]++;
            return total;
        }

        internal void SetModuleCount(ArchonModule moduleType, int count)
        {
            using var log = NewModLog();
            //var tm = GetTorpedoMark();
            //var bm = GetBatteryMark();
            //var dm = GetDriveMark();
            var rm = RepairModule.GetFrom(this);
            var dm = GetTotalDockingCapacity();
            moduleCounts[(int)moduleType] = count;
            //var tm2 = GetTorpedoMark();
            //var bm2 = GetBatteryMark();
            //var dm2 = GetDriveMark();
            var rm2 = RepairModule.GetFrom(this);
            var dm2 = GetTotalDockingCapacity();
            Control.maxDockedVehicles = dm2;
            if (!destroyed && hadUnpausedFrame)
            {
                //if (tm != tm2)
                //    ErrorMessage.AddMessage(string.Format(Language.main.Get($"torpedoCapChanged"), VehicleName, Language.main.Get("cap_t_" + tm2)));
                //if (bm != bm2)
                //    ErrorMessage.AddMessage(string.Format(Language.main.Get($"batteryCapChanged"), VehicleName, Language.main.Get("cap_b_" + bm2)));
                if (dm != dm2)
                    ErrorMessage.AddMessage(Language.main.GetFormat($"Modules.DockingCapacityChanged", VehicleName, dm2));
                if (rm != rm2)
                    ErrorMessage.AddMessage(Language.main.GetFormat($"Modules.RepairCapacityChanged", VehicleName, Language.main.Get("Capacity." + rm2)));
            }
            log.Write($"Changed module counts of {moduleType} to {moduleCounts[(int)moduleType]}");
        }

        internal void EnterFromDocking()
        {
            using var log = NewModLog();
            log.Write(nameof(EnterFromDocking));
            SuspendAutoLeveling();

            var dockingHatchEntry = transform.Find("Docking Hatch/Exit");
            if (dockingHatchEntry)
            {
                log.Write($"Docking hatch entry found at {dockingHatchEntry.position}");
                PlayerEntry(new VehicleHatchDefinition(gameObject, dockingHatchEntry, dockingHatchEntry, dockingHatchEntry));
                //Player.main.transform.position = dockingHatchEntry.position;
                //Player.main.transform.rotation = dockingHatchEntry.rotation;
            }
            else
            {
                log.Error($"Docking hatch entry not found. Entering helm");
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


        [SerializeField]
        private MaterialReactor? reactor;

        internal void SuspendAutoLeveling()
        {
            exitLimitsSuspended = true;
        }
        internal void RestoreAutoLeveling()
        {
            exitLimitsSuspended = false;
        }


        public override string vehicleDefaultName => "Archon";

        /// <summary>
        /// The prefab IDs of submarines declared docked during saving, restored during loading.
        /// </summary>
        public IReadOnlyList<string>? DockedSubPrefabIds { get; private set; }

        protected override SubmarineComposition GetSubmarineComposition()
        {
            using var log = SmartLog.For(Owner);
            var voiceLibraries = transform.GetComponentsInChildren<VoiceLibrary>();
            if (voiceLibraries.Length == 0)
            {
                log.Error("Voice libraries not found. Autopilot will not have a voice");
            }
            else
            {
                foreach (var voiceLibrary in voiceLibraries)
                {
                    log.Write($"Registering voice library {voiceLibrary.voiceName}");
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
                        log.Error("Hatch children not found of " + hatch);
                        continue;
                    }
                    hatchList.Add(new VehicleHatchDefinition(
                        hatch: hatch.gameObject,
                        exit: exit,
                        surfaceExit: exit,
                        entry: entry)
                    );
                }
                log.Write($"Detected {hatchList.Count} hatch(es)");
            }



            var storageRootTransform = transform.Find("StorageRoot");
            if (storageRootTransform.IsNull())
            {
                log.Warn($"Storage root not found. Creating new one");
                storageRootTransform = new GameObject("StorageRoot").transform;
                storageRootTransform.parent = transform;
                storageRootTransform.localPosition = Vector3.zero;
            }
            else
            {
                log.Write($"Found storage root {storageRootTransform.NiceName()}");
            }


            var modularStorageList = new List<VehicleStorage>();
            if (storageRootTransform)
            {
                for (int i = 0; i < 8; i++)
                {
                    var name = $"Storage{i}";
                    var storageTransform = storageRootTransform.Find(name);
                    if (storageTransform.IsNull())
                    {
                        storageTransform = new GameObject(name).transform;
                        storageTransform.parent = storageRootTransform.transform;
                        storageTransform.localPosition = M.V3(i);
                        //Log.Write($"Creating new storage transform {storageTransform} in {storageRootTransform} @{storageTransform.localPosition} => {storageTransform.position}");
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
            var mwps = new List<MobileWaterPark>();


            var waterTank = transform.Find("Interior/Water Tank");
            if (waterTank != null)
            {
                var content = waterTank.Find("Content").OrRequired(() => waterTank);

                mwps.Add(new MobileWaterPark(
                    displayName: AVS.Localization.Text.Translated("Component.WaterTank"),
                    root: waterTank.gameObject,
                    contentContainer: content,
                    height: 8,
                    width: 8
                ));
            }
            else
            {
                log.Write($"Water tank not found");
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
            var upgradeContainer = transform.Find("Interior/Upgrade Panel");
            var ui = upgradeContainer.Find("Panel");
            var plugs = upgradeContainer.Find("Slots");

            var plugProxies = new List<Transform>();
            if (plugs)
            {
                for (int i = 0; i < plugs.childCount; i++)
                {
                    var plug = plugs.GetChild(i);
                    var position = plug.Find("Proxy");
                    if (position != null)
                        plugProxies.Add(position);
                    else
                        log.Write($"Plug {plug.NiceName()} does not have a 'Proxy' child");
                }
            }
            else
                log.Write($"Plugs not found");

            log.Write($"Determined {plugProxies.Count} upgrade panel plug(s)");

            if (ui)
            {
                upgrades.Add(new VehicleUpgrades(
                    @interface: ui.gameObject,
                    flap: ui.gameObject,
                    openAngles: ui.eulerAngles,
                    closedAngles: ui.eulerAngles,
                    plugProxies
                ));
            }
            else
                log.Write($"Upgrades interface not found");

            var batteries = new List<VehicleBatteryDefinition>();


            var cells = transform.Find("Interior/Power Cell Panel/Cells");

            if (cells)
            {
                for (int i = 0; i < cells.childCount; i++)
                {
                    var b = cells.GetChild(i);
                    var slot = b.Find("Slot");
                    if (slot.IsNull())
                        log.Warn($"Power cell slot not found in {b.NiceName()}");
                    if (b != null)
                    {
                        batteries.Add(new VehicleBatteryDefinition(
                            root: b.gameObject,
                            batteryProxy: slot.OrRequired(b)
                        ));
                    }
                }
            }
            else
                log.Write($"Unable to locate 'Interior/Power Cell Panel/Cells' child");

            var helms = new List<Helm>();
            var helm = transform.Find("Helm");
            if (helm)
            {
                var seat = helm.Find("Seat");
                if (!seat)
                {
                    log.Error($"Helm seat not found in {helm.NiceName()}");
                    seat = helm; //use helm as player control location
                }
                else
                {
                    log.Write($"Helm seat found at {seat.position}");
                }
                var helmExit = helm.Find($"ExitLocation");
                if (!helmExit)
                    log.Write($"Helm exit not found for {helm.NiceName()}");

                helms.Add(new Helm
                (
                    root: seat.gameObject,
                    playerControlLocation: helm.gameObject,
                    exitLocation: helmExit,
                    isSeated: true
                ));
            }
            else
                log.Error("Helm not found");

            var tetherSources = new List<GameObject>();
            var tether = transform.Find("Tether");
            if (!tether)
            {

                log.Error("Tether not found. No tethers will be defined");

            }
            else
            {
                foreach (Transform trans in tether)
                {
                    var t = trans.GetComponent<SphereCollider>();
                    if (!t)
                    {
                        log.Error($"Tether {trans} does not hace a sphere collider");
                        continue;
                    }
                    t.radius = t.transform.localScale.x;
                    t.transform.localScale = Vector3.one;
                    tetherSources.Add(t.gameObject);
                }
                log.Write($"Recorded {tetherSources.Count} tether source(s)");
            }





            //Log.Write($"Assigning new engine");
            engine = gameObject.EnsureComponent<MassDrive>();





            return new SubmarineComposition(
                engine: engine,
                hatches: hatchList,
                collisionModel: [transform.Find("CollisionModel").gameObject],
                boundingBoxCollider: transform.Find("EntireBoundingBox").GetComponent<BoxCollider>(),
                storageRootObject: storageRootTransform.gameObject,
                modularStorages: modularStorageList,
                innateStorages: [],
                waterClipProxies: waterClipProxies,
                upgrades: upgrades,
                batteries: batteries,
                tetherSources: tetherSources,
                modulesRootObject: GetOrCreateDefaultModulesRootObject(),
                helms: helms,
                waterParks: mwps
                );




        }

        void IAutopilotEventListener.Signal(AutopilotEvent autopilotEvent)
        {
            using var log = NewModLog();
            log.Write($"Received autopilot event {autopilotEvent}");

            switch (autopilotEvent)
            {
                case AutopilotEvent.PlayerEntry:
                    {
                        VoiceLibrary? voiceLibrary = null;
                        if (ArchonModController.PluginConfig.voice != Voice.Off)
                            VoiceLibraries.TryGetValue(ArchonModController.PluginConfig.voice.ToString(), out voiceLibrary);
                        bool isCombined = false;
                        var voices = voiceLibrary?.GetRandomWelcome(out isCombined).ToList();
                        List<float> gaps = new List<float>();

                        if (voices != null)
                        {
                            for (int i = 0; i + 1 < voices.Count; i++)
                            {
                                gaps.Add(0.1f);
                            }
                            if (Autopilot is
                                {
                                    HealthStatus: AutopilotStatus.HealthSafe,
                                    PowerStatus: AutopilotStatus.PowerSafe,
                                    DepthStatus: AutopilotStatus.DepthSafe
                                })
                            {
                                if (!isCombined) //combined welcome does not blend well with status green voice
                                {
                                    gaps.Add(1);
                                    voices.Add(voiceLibrary?.GetRandomAllSystemsGreen());
                                }
                            }
                            else
                            {
                                gaps.Add(1);
                                switch (Autopilot.HealthStatus)
                                {
                                    case AutopilotStatus.HealthCritical:
                                        voices.Add(voiceLibrary?.GetRandomHealthCritical());
                                        break;
                                    case AutopilotStatus.HealthLow:
                                        voices.Add(voiceLibrary?.GetRandomHealthLow());
                                        break;
                                }
                                switch (Autopilot.PowerStatus)
                                {
                                    case AutopilotStatus.PowerCritical:
                                        voices.Add(voiceLibrary?.GetRandomPowerCritical());
                                        break;
                                    case AutopilotStatus.PowerLow:
                                        voices.Add(voiceLibrary?.GetRandomPowerLow());
                                        break;
                                }
                                switch (Autopilot.DepthStatus)
                                {
                                    case AutopilotStatus.DepthBeyondCrush:
                                        if (voiceLibrary != null)
                                            voices.AddRange(voiceLibrary.GetRandomDepthCritical());
                                        break;
                                    case AutopilotStatus.DepthNearCrush:
                                        voices.Add(voiceLibrary?.GetRandomDepthDangerous());
                                        break;
                                }
                            }
                        }
                        VoiceQueue.Play(new VoiceLine(voices, gaps, "Subtitle.Voice.Welcome"));

                    }
                    break;
            }
        }

        void IAutopilotEventListener.Signal(AutopilotStatusChange statusChange)
        {
            Log.Write($"Received autopilot event {statusChange.NewStatus}");

            VoiceLibrary? voiceLibrary = null;
            if (ArchonModController.PluginConfig.voice != Voice.Off)
                VoiceLibraries.TryGetValue(ArchonModController.PluginConfig.voice.ToString(), out voiceLibrary);

            {
                switch (statusChange.NewStatus)
                {
                    case AutopilotStatus.DepthNearCrush:
                        if (statusChange.PreviousStatus < AutopilotStatus.DepthNearCrush)
                        {
                            var voice = voiceLibrary?.GetRandomDepthDangerous();
                            VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Depth.Dangerous", 1));
                        }
                        break;
                    case AutopilotStatus.DepthBeyondCrush:
                        if (statusChange.PreviousStatus < AutopilotStatus.DepthBeyondCrush)
                        {
                            var voices = voiceLibrary?.GetRandomDepthCritical();
                            VoiceQueue.Play(new VoiceLine(voices, null, "Subtitle.Voice.Depth.Critical", 2));
                        }
                        break;
                    case AutopilotStatus.HealthCritical:
                        if (statusChange.PreviousStatus < AutopilotStatus.HealthCritical)
                        {
                            var voice = voiceLibrary?.GetRandomHealthCritical();
                            VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Health.Critical", 2));

                        }
                        break;
                    case AutopilotStatus.HealthLow:
                        if (statusChange.PreviousStatus < AutopilotStatus.HealthLow)
                        {
                            var voice = voiceLibrary?.GetRandomHealthLow();
                            VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Health.Dangerous", 1));

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
                            var voice = voiceLibrary?.GetRandomPowerLow();
                            VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Power.Dangerous", 1));
                        }
                        break;
                    case AutopilotStatus.PowerCritical:
                    case AutopilotStatus.PowerDead:
                        if (statusChange.PreviousStatus < AutopilotStatus.PowerCritical)
                        {
                            var voice = voiceLibrary?.GetRandomPowerCritical();
                            VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Power.Critical", 2));
                        }
                        break;
                }
                Log.Write($"Received autopilot status change: {statusChange.PreviousStatus} -> {statusChange.NewStatus}");
            }
        }

        internal bool IsDockedBySavegame(PrefabIdentifier? prefabIdentifier)
        {
            using var log = NewModLog();
            if (DockedSubPrefabIds.IsNull())
            {
                log.Write($"No docked vehicles restored from last load operation");
                return false;
            }
            if (prefabIdentifier.IsNull())
            {
                log.Error($"Candidate has no PrefabIdentifier");
                return false;
            }
            if (!DockedSubPrefabIds.Contains(prefabIdentifier.Id))
            {
                log.Error($"Prefab ID {prefabIdentifier.Id} is not declared in list of docked prefab IDs");
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
        //        if (root.IsNull())
        //            return rs;
        //        for (int i = 0; i < 8; i++)
        //        {
        //            var name = $"Storage{i}";
        //            var storageTransform = root.transform.Find(name);
        //            if (storageTransform.IsNull())
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
        //        //if (headLights.IsNull())
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
        //        if (tetherSources.IsNull())
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
