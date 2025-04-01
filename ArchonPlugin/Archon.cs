using FMOD.Studio;
using FMODUnity;
using Subnautica_Archon.MaterialAdapt;
using Subnautica_Archon.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using VehicleFramework;
using VehicleFramework.Engines;
using VehicleFramework.VehicleParts;
using VehicleFramework.VehicleTypes;


namespace Subnautica_Archon
{



    public class Archon : Submarine, IPowerListener
    {
        public static GameObject model;
        private ArchonControl control;
        public ArchonControl Control => control;

        public static readonly Color defaultBaseColor = new Color(0xDE, 0xDE, 0xDE) / 255f;
        public static readonly Color defaultStripeColor = new Color(0x3F, 0x4C, 0x7A) / 255f;

        private List<GameObject> tetherSources;
        //tracks true if vehicle death was ever determined. Can't enter in this state
        private bool wasDead;
        private bool destroyed;
        private float deathAge;
        //private MyLogger Log { get; }
        private MassDrive engine;
        private AutoPilot autopilot;
        private PropertyInfo autoLevelProperty;
        private EnergyInterface energyInterface;
        private DateTime isAutoLevelingSince;
        private int[] moduleCounts = new int[Enum.GetValues(typeof(ArchonModule)).Length];
        public Archon()
        {
            //Log = new MyLogger(this);
            Log.Write($"Constructed");
            MaterialFixer = new MaterialFixer(this, Logging.Default);
        }

        public override float ExitVelocityLimit => 100f;    //any speed is good

 

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
                Log.Write(nameof(GetAssets)+" done");
            }
            catch (Exception ex)
            {
                Log.Write(nameof(GetAssets), ex);
            }
        }

        void OnDestroy()
        {
            Log.Write($"{VehicleName} "+nameof(OnDestroy));
            destroyed = true;
        }


        private bool isInitialized = false;

        public override void SubConstructionComplete()
        {
            base.SubConstructionComplete();
            SetBaseColor(Vector3.zero, defaultBaseColor);
            SetStripeColor(Vector3.zero, defaultStripeColor);
        }

        public override void Awake()
        {
            worldForces.aboveWaterDrag = worldForces.underwaterDrag = 0;


            BayControl.OnDockingFailedFull = (archon, d) =>
            {
                VehicleFramework.Logger.PDANote("Cannot dock: Hangar is full", 3f);
            };

            BayControl.OnDockingFailedTooLarge = (archon, d) =>
            {
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

            Destroy(modulesRoot);

            modulesRoot = control.hangarRoot.gameObject.AddComponent<ChildObjectIdentifier>();


            base.Awake();
            var cameraController = gameObject.GetComponentInChildren<VehicleFramework.VehicleComponents.MVCameraController>();
            if (cameraController != null)
            {
                Log.Write($"Destroying camera controller {cameraController}");
                Destroy(cameraController);
            }


        }

        private void OnQuickbarToggle(int slotID, bool state)
        {
            if (state == true)
            {
                var slotId = slotIDs[slotID];
                var item = modules.GetItemInSlot(slotId)?.item;
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
                            Log.Write($"Removing quick bar item");
                            modules.RemoveItem(slotId, true, true);

                            Log.Write($"Undocking {Log.Describe(vehicle)}");
                            control.Undock(vehicle.gameObject);
                            ToggleSlot(slotID, false);
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

        private void LocalInit()
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
                        autoLevelProperty = autopilot.GetType().GetProperty("autoLeveling",BindingFlags.Instance | BindingFlags.NonPublic);
                        if (autoLevelProperty is null)
                            Log.Error($"Could not find autoLeveling property on {autopilot}");


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
                    //rotateCamera = GetComponentInChildren<RotateCamera>();

                    //if (rotateCamera == null)
                    //    EchLog.Write($"Rotate camera not found");
                    //else
                    //    EchLog.Write($"Found camera rotate {rotateCamera.name}");

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


                LocalInit();

                base.Start();
                Log.Write(nameof(Start)+" done");

            }
            catch (Exception ex)
            {
                Log.Write(nameof(Start), ex);
            }
        }

        private readonly List<MonoBehaviour> reenableOnExit = new List<MonoBehaviour>();

        public override void PlayerEntry()
        {
            control.Enter(Helper.GetPlayerReference());
            pingInstance.enabled = false;
            base.PlayerEntry();
        }

        public override void PlayerExit()
        {
            base.PlayerExit();
            pingInstance.enabled = true;
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

                Log.Write(nameof(BeginPiloting));
                LocalInit();

                base.BeginPiloting();
                control.Control(Helper.GetPlayerReference());

                reenableOnExit.Clear();


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

                LocalInit();
                control.ExitControl(Helper.GetPlayerReference(), !exitLimitsSuspended);
                control.isAutoLeveling = false;
                base.StopPiloting();

                if (Player.main.sitting)
                {
                    Log.Error($"Player is still sitting after control exit");
                    Player.main.sitting = false;
                    Player.main.playerController.ForceControllerSize();
                }
                else
                    Log.Write($"Sitting not detected");

                foreach (MonoBehaviour behavior in reenableOnExit)
                {
                    Log.Write($"Reenabling {behavior.name}");
                    behavior.enabled = true;
                }

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


        public override void FixedUpdate()
        {
            try
            {
                LocalInit();
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

                float level = 1;

                float recharge =
                      0.4f  //max 1.6 per second
                    * level;

                energyInterface.ModifyCharge(
                    Time.deltaTime
                    * recharge
                    );
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
                var level = 1;// RepairModule.GetRelativeSelfRepair(RepairModule.GetFrom(this));

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

        private void ProcessBoost(bool lowPower)
        {

            var boostToggle = !MainPatcher.PluginConfig.holdToBoost;

            //engine.driveUpgrade = HighestModuleType(ArchonModule.DriveMk1, ArchonModule.DriveMk2, ArchonModule.DriveMk3);

            //if (GameInput.GetButtonDown(GameInput.Button.Sprint) && boostToggle)
            //{
            //    if (control.forwardAxis > 0 && engine.overdriveActive > 0)
            //        engine.overdriveActive = 0;
            //}

            bool canBoost =
                !lowPower
                ;

            if (boostToggle)
            {
                if (control.forwardAxis <= 0 || !canBoost)
                    engine.overdriveActive = 0;
                else
                    engine.overdriveActive = Mathf.Max(engine.overdriveActive, GameInput.GetAnalogValueForButton(GameInput.Button.Sprint));
            }
            else
                engine.overdriveActive = control.forwardAxis > 0 && canBoost
                    ? GameInput.GetAnalogValueForButton(GameInput.Button.Sprint)
                    : 0;


            control.overdriveActive = engine.overdriveActive > 0.5f;
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
                    if (control.lights )
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

        public override void OnVehicleDocked(Vehicle vehicle, Vector3 exitLocation)
        {
            base.OnVehicleDocked(vehicle, exitLocation);
            SetBaseColor(Vector3.zero, nonBlackBaseColor);
            SetStripeColor(Vector3.zero, nonBlackStripeColor);
        }

        private static float SecondaryEulerZeroDistance(float euler)
        {
            return euler > 180f
                ? 360f - euler  //mirror around
                : euler;
        }

        public override void Update()
        {
            try
            {
                LocalInit();



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

                try
                {
                    if (autoLevelProperty != null)
                    {
                        bool autoLeveling = (bool)autoLevelProperty.GetValue(autopilot);
                        if (autoLeveling)
                        {
                            var rollDelta = SecondaryEulerZeroDistance(transform.eulerAngles.z);
                            var pitchDelta = SecondaryEulerZeroDistance(transform.eulerAngles.x);

                            //Log.Write($"Angle error at {rollDelta} / {pitchDelta}");
                            if (!control.isAutoLeveling)
                            {
                                isAutoLevelingSince = DateTime.Now;
                                control.isAutoLeveling = true;
                            }
                            else if (DateTime.Now - isAutoLevelingSince > TimeSpan.FromSeconds(5))
                            {
                                autoLevelProperty.SetValue(autopilot, false);
                                control.isAutoLeveling = false;
                                ErrorMessage.AddError($"Auto-leveling has not succeeded in 5 seconds. Aborting auto-level");
                            }
                        }
                        else if (control.isAutoLeveling)
                        {
                            DeselectSlots();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to read autoleveling property");
                    Debug.LogException(ex);
                }


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

                ProcessBoost(lowPower);
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
            PlayerEntry();
            BeginPiloting();

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
        internal void SuspendExitLimits()
        {
            exitLimitsSuspended = true;
        }
        internal void RestoreExitLimits()
        {
            exitLimitsSuspended = false;
        }

        public string VehicleName => subName ? subName.GetName() : vehicleName;

        public override int MaxHealth => 2000;
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
                        Log.Error("Hatch children not found of "+hatch);
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
                var clipProxyParent = transform.Find("WaterClipProxy");
                var rs = new List<GameObject>();
                if (clipProxyParent != null)
                {
                    for (int i = 0; i < clipProxyParent.childCount; i++)
                        rs.Add(clipProxyParent.GetChild(i).gameObject);
                }
                else
                    Log.Write("Clip proxy not found");
                return rs;
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
