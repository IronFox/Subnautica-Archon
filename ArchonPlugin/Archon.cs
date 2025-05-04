using FMOD.Studio;
using FMODUnity;
using Subnautica_Archon.MaterialAdapt;
using Subnautica_Archon.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using VehicleFramework;
using VehicleFramework.Engines;
using VehicleFramework.VehicleComponents;
using VehicleFramework.VehicleParts;
using VehicleFramework.VehicleTypes;
using Logger = VehicleFramework.Logger;


namespace Subnautica_Archon
{



    public class Archon : Submarine, IPowerListener, IProtoTreeEventListener
    {
        public static GameObject model;
        private ArchonControl control;
        public ArchonControl Control => control;

        public static readonly Color defaultBaseColor = new Color(0xDE, 0xDE, 0xDE) / 255f;
        public static readonly Color defaultStripeColor = new Color(0x3F, 0x4C, 0x7A) / 255f;

        private List<GameObject> tetherSources;
        //tracks true if vehicle death was ever determined. Can't enter in this state
        private bool wasDead;
        public bool destroyed;
        private float deathAge;
        //private MyLogger Log { get; }
        private MassDrive engine;
        private AutoPilot autopilot;
        private EnergyInterface energyInterface;
        private int[] moduleCounts = new int[Enum.GetValues(typeof(ArchonModule)).Length];

        private bool clippingWater;

        public Archon()
        {
            //Log = new MyLogger(this);
            Log.Write($"Constructed");
            MenuTracker = new MenuTracker(() =>
            {
                if (control)
                    control.PrepareForSaving();
            }, () => { });
            MaterialFixer = new MaterialFixer(this, Logging.Verbose);
        }

        public override float ExitVelocityLimit => 100f;    //any speed is good



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
            Log.Write($"Comparing colors {baseColor} and {stripeColor}");
            if (baseColor == Color.white && stripeColor == Color.white)
            {
                Log.Write($"Resetting white {VehicleName}");
                SetBaseColor(Vector3.zero, defaultBaseColor);
                SetStripeColor(Vector3.zero, defaultStripeColor);
            }
        }

        public static Sprite saveFileSprite, moduleBackground;
        public static Atlas.Sprite craftingSprite, pingSprite;
        public static Atlas.Sprite emptySprite = new Atlas.Sprite(Texture2D.blackTexture);
        public override Atlas.Sprite CraftingSprite => craftingSprite ?? base.CraftingSprite;
        public override Atlas.Sprite PingSprite => pingSprite ?? base.PingSprite;
        public override Sprite SaveFileSprite => saveFileSprite ?? base.SaveFileSprite;
        public override Sprite ModuleBackgroundImage => moduleBackground ?? base.ModuleBackgroundImage;
        public override string Description => Language.main.Get("description");
        public override string EncyclopediaEntry => Language.main.Get("encyclopedia");

        public override Dictionary<TechType, int> Recipe =>
            new Dictionary<TechType, int> {
                { TechType.PowerCell, 1 },
                { TechType.AdvancedWiringKit, 2 },
                //{ TechType.UraniniteCrystal, 3 },
                //{ TechType.Lead, 3 },
                { TechType.Diamond, 2 },
                //{ TechType.Kyanite, 2 },
                { TechType.PlasteelIngot, 4 },
            };


        public static void GetAssets()
        {
            try
            {
                Log.Write(nameof(GetAssets));
                var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string bundlePath;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    bundlePath = Path.Combine(modPath, "archon.osx");
                else
                    bundlePath = Path.Combine(modPath, "archon");
                Log.Write($"Trying to load asset bundle from '{bundlePath}'");
                if (!File.Exists(bundlePath))
                    Log.Write("This file does not appear to exist");
                var bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle != null)
                {
                    var assets = bundle.LoadAllAssets();
                    foreach (var obj in assets)
                    {
                        Log.Write("Scanning object: " + obj.name);
                        if (obj.name == "Archon")
                        {
                            model = (GameObject)obj;
                        }
                    }
                    if (model == null)
                        Log.Write("Model not found among: " + string.Join(", ", Helper.Names(assets)));
                }
                else
                    Log.Write("Unable to loade bundle from path");
                Log.Write(nameof(GetAssets) + " done");
            }
            catch (Exception ex)
            {
                Log.Write(nameof(GetAssets), ex);
            }
        }

        void OnDestroy()
        {
            Log.Write($"{VehicleName} " + nameof(OnDestroy));
            destroyed = true;
        }


        private bool isInitialized = false;
        private bool hadUnpausedFrame = false;

        public override void SubConstructionComplete()
        {
            base.SubConstructionComplete();
            SetBaseColor(Vector3.zero, defaultBaseColor);
            SetStripeColor(Vector3.zero, defaultStripeColor);
        }

        public override void Awake()
        {
            Log.Write(nameof(Awake));
            worldForces.aboveWaterDrag = worldForces.underwaterDrag = 0;
            CyclopsHelper.Start();



            BayControl.OnDockingFailedFull = (archon, d) =>
            {
                Log.Write($"full");
                VehicleFramework.Logger.PDANote("Cannot dock: Hangar is full", 3f);
            };

            BayControl.OnDockingFailedTooLarge = (archon, d) =>
            {
                Log.Write($"too large");
                VehicleFramework.Logger.PDANote("Cannot dock: Your vehicle is too large", 3f);
            };

            onToggle += OnQuickbarToggle;

            var existing = GetComponent<VFEngine>();
            if (existing != null)
            {
                Log.Write($"Removing existing vfEngine {existing}");
                //HierarchyAnalyzer analyzer = new HierarchyAnalyzer();
                Destroy(existing);
            }
            VFEngine = Engine = engine = gameObject.AddComponent<MassDrive>();
            Log.Write($"Assigned new engine");

            control = GetComponent<ArchonControl>();
            control.freeCamera = MainPatcher.PluginConfig.defaultToFreeCamera;


            //var loadSave = gameObject.GetComponent<LoadSaveComponent>();
            //if (!loadSave)
            //    loadSave = gameObject.AddComponent<LoadSaveComponent>();
            //loadSave.control = control;

            Destroy(modulesRoot);

            modulesRoot = control.hangarRoot.gameObject.AddComponent<ChildObjectIdentifier>();

            var interior = transform.Find("Interior");
            if (interior)
            {
                var reactorTransform = interior.Find("Biofuel Storage");
                if (reactorTransform)
                {
                    var reactor = reactorTransform.gameObject.AddComponent<MaterialReactor>();
                    reactor.Initialize(this, 10, 8, "Archon Reactor", 0, MaterialReactor.GetBioReactorData());
                    reactor.canViewWhitelist = false;
                    reactor.interactText = "Archon Bioreactor";
                }
                else
                    Log.Error("Unable to find Biofuel Storage child");
            }
            else
                Log.Error("Unable to find Interior child");


            base.Awake();

            Log.Write("Checking quickslots");
            foreach (var s in QuickSlots)
            {
                var mod = modules.GetItemInSlot(s.ID);
                if (mod != null && mod.item == null)
                {
                    Log.Error($"Found invalid item in slot {s}. Purging");
                    modules.RemoveItem(s.ID, true, false);
                }
            }


            var cameraController = gameObject.GetComponentInChildren<VehicleFramework.VehicleComponents.MVCameraController>();
            if (cameraController)
            {
                Log.Write($"Destroying camera controller {cameraController}");
                Destroy(cameraController);
            }


        }

        private void OnQuickbarToggle(int slotID, bool state)
        {
            if (state == true)
            {
                var slotId = new QuickSlot(slotID, slotIDs[slotID]);
                var item = modules.GetItemInSlot(slotId.ID)?.item;
                if (item == null)
                    Log.Error($"No item found in slot {slotID}/{slotId}");
                else
                {
                    var vehicle = item.gameObject.GetComponent<Vehicle>();
                    if (!vehicle)
                        Log.Error($"Item found in slot {slotID}/{slotId} ({item.gameObject}) is not a vehicle");
                    else
                    {
                        var cr = control.CheckUndocking(vehicle.gameObject);
                        if (cr == UndockingCheckResult.Ok)
                        {
                            AbortAutoLeveling();
                            Log.Write($"Removing quick bar item in slot [{slotId}]");
                            var removed = modules.RemoveItem(slotId.ID, true, true);
                            Log.Write($"Removed [{removed}]");


                            Log.Write($"Undocking {Log.Describe(vehicle)}");
                            control.Undock(vehicle.gameObject);
                            ToggleSlot(slotID, false);
                            if (vehicle is Drone)
                                SignalQuickslotsChangedWhilePiloting(slotId);
                        }
                        else
                        {
                            ToggleSlot(slotID, false);
                            ErrorMessage.AddError($"Cannot undock right now ({cr})");
                        }
                    }
                }
            }

        }

        public override bool AutoApplyShaders => false;
        public override bool DoesAutolevel => false;

        private Coroutine autoLevelRoutine;
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
                control.doAutoLevel = false;
                Log.Write("Aborted. Control restored");
                return true;
            }
            return false;
        }

        private IEnumerator AutoLevelThenExit()
        {
            if (control.IsLevel)
            {
                Log.Write("Archon is level. Exiting now");
                base.DeselectSlots();
                autoLevelRoutine = null;
                yield break;
            }

            Log.Write("Archon is not level. Leveling out");
            control.doAutoLevel = true;
            Logger.PDANote($"Leveling out. Please stand by");
            //var timewindow = TimeSpan.FromSeconds(5);
            //var deadline = DateTime.Now + timewindow;
            float timewindow = 5;
            var remaining = timewindow;
            while (control.doAutoLevel && !control.IsLevel && remaining > 0)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }
            Log.Write("Archon is level or deadline has passed");
            autoLevelRoutine = null;
            if (control.doAutoLevel)
            {
                Log.Write("Archon leveling has not been aborted");
                control.doAutoLevel = false;
                if (control.IsLevel)
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
                    autopilot = GetComponentInChildren<AutoPilot>();

                    if (autopilot)
                    {
                        //"Airon" - weird, partially indecipherable low energy voice
                        //"Chels-E" - high-pitched panicky
                        //"Mikjaw"/"Salli" - just bad
                        //"Turtle" - missing?
                        //autopilot.apVoice.voice = VoiceManager.GetVoice("Salli");
                        autopilot.apVoice.voice = Helper.Clone(autopilot.apVoice.voice);
                        autopilot.apVoice.voice.PowerLow = null;
                        autopilot.apVoice.voice.BatteriesNearlyEmpty = null;
                        autopilot.apVoice.voice.UhOh = null;

                    }

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
                    control.RedetectDocked();
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
                    Log.Write("LocalInit()", e);
                }

            }
        }


        public override void SetBaseColor(Vector3 hsb, Color color)
        {
            Log.Write($"Updating sub base color to {color}");
            base.SetBaseColor(hsb, color);

            var listeners = GetComponentsInChildren<IColorListener>();
            foreach (var listener in listeners)
                listener.SetColors(baseColor, stripeColor);

        }

        public override void SetStripeColor(Vector3 hsb, Color color)
        {
            Log.Write($"Updating sub stripe color to {color}");
            base.SetStripeColor(hsb, color);

            var listeners = GetComponentsInChildren<IColorListener>();
            foreach (var listener in listeners)
                listener.SetColors(baseColor, stripeColor);
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
                Log.Write(nameof(Start), ex);
            }
        }



        public override void PlayerEntry()
        {
            Log.Write(nameof(PlayerEntry));
            control.Enter(Helper.GetPlayerReference(), skipOrientation: exitLimitsSuspended || !hadUnpausedFrame);
            pingInstance.SetHudIcon(false);

            base.PlayerEntry();
        }

        public override void PlayerExit()
        {
            base.PlayerExit();
            pingInstance.SetHudIcon(true);
            control.Exit();

        }



        public override void BeginPiloting()
        {
            try
            {
                if (!liveMixin.IsAlive() || wasDead)
                {
                    ErrorMessage.AddError(string.Format(Language.main.Get("destroyedAndCannotBeBoarded"), VehicleName));
                    return;
                }
                if (refreshQuickslotsOnControl.HasValue)
                {
                    var v = refreshQuickslotsOnControl.Value;
                    control.PrepareForSaving();
                    refreshQuickslotsOnControl = null;
                    //SignalQuickslotsChangedWhilePiloting(v);
                }


                Log.Write(nameof(BeginPiloting));
                LazyInit();

                base.BeginPiloting();
                control.Control(Helper.GetPlayerReference());


                //playerPosition = Player.main.transform.parent.gameObject;
            }
            catch (Exception ex)
            {
                Log.Write(nameof(BeginPiloting), ex);
            }
        }

        public override void StopPiloting()
        {
            try
            {
                Log.Write(nameof(StopPiloting));

                LazyInit();
                control.ExitControl(Helper.GetPlayerReference(), skipOrientation: exitLimitsSuspended);
                base.StopPiloting();

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
                Log.Write(nameof(StopPiloting), ex);
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
            var seamoth = SeamothHelper.Seamoth;

            if (clipProxyParent && seamoth)
            {
                WaterClipProxy seamothWCP = SeamothHelper.Seamoth.GetComponentInChildren<WaterClipProxy>();

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
                Log.Write($"Water-clip proxies adapted ({enable})");

            }
            else
                Log.Write("Clip proxies or seamoth not found. Can't adjust right now");
        }

        public bool ClipWater => control.IsBoarded && !control.IsBeingControlled;


        public override void FixedUpdate()
        {
            try
            {
                LazyInit();

                if (clippingWater != ClipWater)
                {
                    SetWaterProxiesEnabled(ClipWater);
                }

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
                    Log.Write(nameof(FixedUpdate), ex);
                }
            }
        }

        private void ProcessEnergyRecharge(out bool lowPower, out bool criticalPower)
        {

            if (energyInterface != null
                && !IngameMenu.main.gameObject.activeSelf)
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
                lowPower = energyCharge < energyCapacity * 0.02f;
                criticalPower = energyCharge < energyCapacity * 0.01f;


            }
            else
            {
                lowPower = false;
                criticalPower = false;
            }

        }

        private void ProcessRegeneration(bool criticalPower)
        {
            control.isHealing = false;

            var delta = Time.deltaTime;

            if (liveMixin != null)
            {
                var level = 0.01f;// RepairModule.GetRelativeSelfRepair(RepairModule.GetFrom(this));

                if (liveMixin.health < liveMixin.maxHealth
                    && liveMixin.IsAlive()
                    && !criticalPower
                    && !IngameMenu.main.gameObject.activeSelf
                    //&& MainPatcher.PluginConfig.selfHealingSpeed > 0
                    && delta > 0
                    && level > 0)
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
                        1 //max 1 energy per second
                        * delta
                        //* MainPatcher.PluginConfig.selfHealingSpeed / 100   //if slower, cost less
                        * effective //if clamped, cost less
                        ;

                    powerMan.TrySpendEnergy(energyDemand);


                    var actuallyHealed = clamped;
                    liveMixin.AddHealth(actuallyHealed);
                    control.isHealing = true;

                }


                control.maxHealth = liveMixin.maxHealth;
                control.currentHealth = liveMixin.health;

            }
        }

        private void ForwardControlAxes()
        {
            if (control.batteryDead || control.powerOff)
            {
                control.forwardAxis = 0;
                control.rightAxis = 0;
                control.upAxis = 0;
            }
            else
            {
                control.forwardAxis = engine.currentInput.z;
                control.rightAxis = engine.currentInput.x;
                control.upAxis = engine.currentInput.y;
            }
        }

        private void ProcessEngine(bool lowPower)
        {
            engine.overdriveActive = 0;
            engine.doNotAccelerate = control.doAutoLevel;
            engine.freeCamera = control.freeCamera;
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
            if (control.IsBeingControlled
                && Player.main.pda.state == PDA.State.Closed
                && !IngameMenu.main.gameObject.activeSelf
                )
            {
                if (GameInput.GetButtonDown(GameInput.Button.RightHand))
                {
                    control.lights = !control.lights;
                    if (control.lights)
                    {
                        lightsOnSound.Stop();
                        lightsOnSound.Play();
                    }
                    else
                    {
                        lightsOffSound.Stop();
                        lightsOffSound.Play();
                    }

                }
            }

        }

        /// <summary>
        /// Redetects proximity to the ocean surface and forwards the state to control
        /// </summary>
        private void RepositionCamera()
        {
            control.UpdateLowCamera(Ocean.GetOceanLevel());
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
            MaterialFixer.OnVehicleUndocked();
        }


        private MaterialFixer MaterialFixer;

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


        private MenuTracker MenuTracker { get; }
        public override void Update()
        {
            try
            {
                LazyInit();
                MenuTracker.Update();
                hadUnpausedFrame |= Time.deltaTime > 0;


                //if (Player.main.sitting)
                //{
                //    Log.Error($"Player is sitting in sub");
                //    Player.main.sitting = false;
                //    Player.main.playerController.ForceControllerSize();
                //}

                if (baseColor != Color.black)
                    nonBlackBaseColor = baseColor;
                if (stripeColor != Color.black)
                    nonBlackStripeColor = stripeColor;

                MaterialFixer.OnUpdate();

                control.flipFreeHorizontalRotationInReverse = MainPatcher.PluginConfig.flipFreeHorizontalRotationInReverse;
                control.flipFreeVerticalRotationInReverse = MainPatcher.PluginConfig.flipFreeVerticalRotationInReverse;

                if (Input.GetKeyDown(KeyCode.F6))
                {
                    //if (Player.main.currentMountedVehicle != null)
                    //{
                    //    HierarchyAnalyzer a = new HierarchyAnalyzer();
                    //    a.LogToJson(Player.main.currentMountedVehicle.transform, $@"C:\temp\vehicle.json");
                    //}

                    Log.Write($"Reapplying materials");
                    MaterialFixer.ReApply();
                }




                if (!liveMixin.IsAlive() || wasDead)
                {
                    wasDead = true;
                    deathAge += Time.deltaTime;
                    if (deathAge > 1.5f)
                    {
                        Log.Write($"Emitting pseudo self destruct");
                        control.SelfDestruct(true);
                        Log.Write($"Calling OnSalvage");
                        OnSalvage();
                        enabled = false;
                        Log.Write($"Done?");
                        return;
                    }
                }

                //ArchonControl.targetArrows = MainPatcher.PluginConfig.targetArrows;

                Vector2 lookDelta = GameInput.GetLookDelta();
                control.lookRightAxis = lookDelta.x * 0.1f;
                control.lookUpAxis = lookDelta.y * 0.1f;

                ProcessEnergyRecharge(out var lowPower, out var criticalPower);
                ProcessRegeneration(criticalPower);
                ForwardControlAxes();
                ProcessTriggers();

                control.outOfWater = !GetIsUnderwater();
                control.cameraCenterIsCockpit = Player.main.pda.state == PDA.State.Opened;

                if (Player.main.pda.state == PDA.State.Closed && !IngameMenu.main.gameObject.activeSelf)
                {
                    control.zoomAxis = -Input.GetAxis("Mouse ScrollWheel")
                        +
                        ((Input.GetKey(MainPatcher.PluginConfig.altZoomOut) ? 1f : 0f)
                        - (Input.GetKey(MainPatcher.PluginConfig.altZoomIn) ? 1f : 0f)) * 0.02f
                        ;
                }

                if (control.IsBeingControlled && GameInput.GetKeyDown(MainPatcher.PluginConfig.toggleFreeCamera))
                    engine.freeCamera = control.freeCamera = !control.freeCamera;

                ProcessEngine(lowPower);
                RepositionCamera();

                if (energyInterface != null)
                {
                    energyInterface.GetValues(out var energyCharge, out var energyCapacity);

                    control.maxEnergy = energyCapacity;
                    control.currentEnergy = energyCharge;
                }

                base.Update();
            }
            catch (Exception ex)
            {
                Log.Write(nameof(Update), ex);
            }
        }


        public void OnPowerUp()
        {
            control.powerOff = false;
        }

        public void OnPowerDown()
        {
            control.powerOff = true;
        }

        public void OnBatteryDead()
        {
            control.batteryDead = true;
        }

        public void OnBatteryRevive()
        {
            control.batteryDead = false;
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
            //var rm = GetSelfRepairMark();
            //moduleCounts[(int)moduleType] = count;
            //var tm2 = GetTorpedoMark();
            //var bm2 = GetBatteryMark();
            //var dm2 = GetDriveMark();
            //var rm2 = GetSelfRepairMark();
            //if (!destroyed)
            //{
            //    if (tm != tm2)
            //        ErrorMessage.AddMessage(string.Format(Language.main.Get($"torpedoCapChanged"), VehicleName, Language.main.Get("cap_t_" + tm2)));
            //    if (bm != bm2)
            //        ErrorMessage.AddMessage(string.Format(Language.main.Get($"batteryCapChanged"), VehicleName, Language.main.Get("cap_b_" + bm2)));
            //    if (dm != dm2)
            //        ErrorMessage.AddMessage(string.Format(Language.main.Get($"boostCapChanged"), VehicleName, Language.main.Get("cap_d_" + dm2)));
            //    if (rm != rm2)
            //        ErrorMessage.AddMessage(string.Format(Language.main.Get($"repairCapChanged"), VehicleName, Language.main.Get("cap_r_" + rm2)));
            //}
            //Debug.Log($"Changed counts of {moduleType} to {moduleCounts[(int)moduleType]}");
        }

        internal void EnterFromDocking()
        {
            Log.Write(nameof(EnterFromDocking));
            SuspendAutoLeveling();
            PlayerEntry();
            BeginPiloting();
            RestoreAutoLeveling();

        }

        public override float ExitPitchLimit
            => exitLimitsSuspended
                ? 360
                : base.ExitPitchLimit;

        public override float ExitRollLimit
            => exitLimitsSuspended
                ? 360
                : base.ExitRollLimit;

        private bool exitLimitsSuspended = false;
        internal void SuspendAutoLeveling()
        {
            exitLimitsSuspended = true;
        }
        internal void RestoreAutoLeveling()
        {
            exitLimitsSuspended = false;
        }

        public void ToggleSlot(QuickSlot slot, bool enabled)
        {
            base.ToggleSlot(slot.Index, enabled);
        }

        private readonly Undoable disabledCameras = new Undoable();
        private QuickSlot? refreshQuickslotsOnControl;
        internal void SignalQuickslotsChangedWhileLoading(QuickSlot slot)
        {
            refreshQuickslotsOnControl = slot;
        }
        internal void SignalQuickslotsChangedWhilePiloting(QuickSlot slot)
        {
            Log.Write(nameof(SignalQuickslotsChangedWhilePiloting));
            if (!control.IsBeingControlled)
            {
                Log.Write($"Not actually piloting. Ignoring");
                return;
            }
            //var qs = uGUI.main.quickSlots;
            //new MethodAdapter<uGUI_ItemIcon, TechType>(qs, "SetForeground")
            //    .Invoke(qs.GetIcon(slot.Index), TechType.None);
            //new MethodAdapter<uGUI_ItemIcon, TechType, bool>(qs, "SetBackground")
            //    .Invoke(qs.GetIcon(slot.Index), TechType.None, false);

            SuspendAutoLeveling();
            base.DeselectSlots();
            RestoreAutoLeveling();
            //foreach (var mbehavior in GetComponentsInChildren<MonoBehaviour>())
            //    SimulateUpdate(mbehavior);
            //foreach (var mbehavior in Player.main.GetComponentsInChildren<MonoBehaviour>())
            //    SimulateUpdate(mbehavior);
            //BeginPiloting();

            Player.main.camRoot
                .GetComponentsInChildren<Camera>()
                .ToEnabled()
                .DisableAllEnabled(disabledCameras);
            StartCoroutine(ReenterNextFrame());
        }

        private IEnumerator ReenterNextFrame()
        {
            yield return null;
            BeginPiloting();
            disabledCameras.UndoAll();
        }

        public string VehicleName => Helper.GetName(this);

        public override int MaxHealth => 20000;
        public override int NumModules => 8;
        public override int BaseCrushDepth => 300;
        public override int CrushDepthUpgrade1 => 200;

        public override int CrushDepthUpgrade2 => 600;

        public override int CrushDepthUpgrade3 => 600;

        public override string vehicleDefaultName => "Archon";



        public override List<VehicleHatchStruct> Hatches
        {
            get
            {
                var hatches = transform.Find("Hatches");
                if (!hatches)
                {
                    Log.Error("Hatches not found");
                    return new List<VehicleHatchStruct>();
                }
                var rs = new List<VehicleHatchStruct>();
                foreach (Transform hatch in hatches)
                {
                    var exit = hatch.Find("Exit");
                    var entry = hatch.Find("Entry");
                    if (!exit || !entry)
                    {
                        Log.Error("Hatch children not found of " + hatch);
                        continue;
                    }
                    rs.Add(new VehicleHatchStruct
                    {
                        Hatch = hatch.gameObject,
                        ExitLocation = exit,
                        SurfaceExitLocation = exit,
                        EntryLocation = entry
                    });
                }
                Log.Write($"Returning {rs.Count} hatch(es)");

                return rs;
            }
        }

        public override GameObject VehicleModel => model;

        public override GameObject CollisionModel => transform.Find("CollisionModel").gameObject;
        public override GameObject BoundingBox => transform.Find("EntireBoundingBox").gameObject;
        public override PilotingStyle pilotingStyle => PilotingStyle.Other;

        public override List<VehicleStorage> ModularStorages
        {
            get
            {
                var root = transform.Find("StorageRoot").gameObject;
                var rs = new List<VehicleStorage>();
                if (root == null)
                    return rs;
                for (int i = 0; i < 8; i++)
                {
                    var name = $"Storage{i}";
                    var storageTransform = root.transform.Find(name);
                    if (storageTransform == null)
                    {
                        storageTransform = new GameObject(name).transform;
                        storageTransform.parent = root.transform;
                        storageTransform.localPosition = M.V3(i);
                        Log.Write($"Creating new storage transform {storageTransform} in {root} @{storageTransform.localPosition} => {storageTransform.position}");
                    }
                    rs.Add(new VehicleStorage
                    {
                        Container = storageTransform.gameObject,
                        Height = 2,
                        Width = 2
                    });
                }
                return rs;

            }
        }
        public override List<GameObject> WaterClipProxies
        {
            get
            {
                return new List<GameObject>();
            }
        }

        public override List<VehicleUpgrades> Upgrades
        {
            get
            {
                var rs = new List<VehicleUpgrades>();
                var ui = transform.Find("UpgradesInterface");
                var plugs = transform.Find("Module Plugs");

                var plugProxies = new List<Transform>();
                if (plugs != null)
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

                if (ui != null)
                {
                    rs.Add(new VehicleUpgrades
                    {
                        Interface = ui.gameObject,
                        Flap = ui.gameObject,
                        ModuleProxies = plugProxies
                    });
                }
                else
                    Log.Write($"Upgrades interface not found");
                return rs;

            }

        }

        public override List<VehicleBattery> Batteries
        {
            get
            {
                var rs = new List<VehicleBattery>();


                var batteries = transform.Find("Batteries");

                if (batteries != null)
                {
                    for (int i = 0; i < batteries.childCount; i++)
                    {
                        var b = batteries.GetChild(i);
                        if (b != null)
                        {
                            rs.Add(new VehicleBattery
                            {
                                BatterySlot = b.gameObject,
                                BatteryProxy = b
                            });
                        }
                    }
                }
                else
                    Log.Write($"Unable to locate 'Batteries' child");
                return rs;
            }

        }


        //public override VFEngine VFEngine { get; set; }

        private List<VehicleFloodLight> headLights = new List<VehicleFloodLight>();

        public override List<VehicleFloodLight> HeadLights
        {
            get
            {
                //Log.Write($"Get HeadLights");
                //if (headLights is null)
                //{

                //    headLights = new List<VehicleFloodLight>();
                //    try
                //    {
                //        var hl = transform.GetComponentsInChildren<Light>();
                //        Log.Write($"processing {hl.Length} headlight(s)");


                //        if (hl.Length > 0)
                //        {
                //            foreach (var light in hl)
                //                if (light.type == LightType.Spot && light.transform.name != "Center Light")
                //                {
                //                    var go = new GameObject($"Light Dummy for {light.name}");
                //                    go.transform.parent = light.transform.parent;
                //                    go.transform.localPosition = light.transform.localPosition;
                //                    go.transform.localRotation = light.transform.localRotation;
                //                    Log.Write($"Reparenting light {light} to {go}");
                //                    light.transform.parent = go.transform;
                //                    light.transform.localPosition = Vector3.zero;
                //                    light.transform.localRotation = Quaternion.identity;
                //                    light.transform.name = light.name = "VolumetricLight";

                //                    headLights.Add(new VehicleFloodLight
                //                    {
                //                        Angle = light.spotAngle,
                //                        Color = light.color,
                //                        Intensity = light.intensity,
                //                        Light = go,
                //                        Range = light.range
                //                    });
                //                }
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Write("HeadLights", ex);
                //    }
                //    Log.Write($"Returning {headLights.Count} headlight(s)");
                //}
                return headLights;

            }

        }

        public override List<VehiclePilotSeat> PilotSeats
        {
            get
            {
                var cockpit = transform.Find("Cockpit");
                var entry = transform.Find("Interior/Control Entry");
                var cockpitExit = entry.Find($"Exit");
                if (!cockpit || !entry || !cockpitExit)
                {
                    Log.Write("Cockpit not found");
                    return default;
                }
                return new List<VehiclePilotSeat>() {  new VehiclePilotSeat
                    {
                        Seat = entry.gameObject,
                        SitLocation = cockpit.gameObject,
                        ExitLocation = cockpitExit,
                        LeftHandLocation = cockpit,
                        RightHandLocation = cockpit,
                    }
                };
            }
        }



        public override List<GameObject> TetherSources
        {
            get
            {
                if (tetherSources is null)
                {
                    tetherSources = new List<GameObject>();
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

                }
                return tetherSources;
            }
        }

    }


}
