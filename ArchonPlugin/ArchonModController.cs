using Assets.Behavior.Adapters;
using AVS;
using AVS.Assets;
using AVS.Log;
using AVS.Patches;
using AVS.UpgradeModules;
using AVS.Util;
using BepInEx;
using HarmonyLib;
using Nautilus.Handlers;
using Subnautica_Archon.Adapters;
using Subnautica_Archon.Components;
using Subnautica_Archon.Modules;
using Subnautica_Archon.Util;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Subnautica_Archon
{



    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, Nautilus.PluginInfo.PLUGIN_VERSION)]
    public class ArchonModController : RootModController
    {
        private static StaticImages? staticImages;
        public static StaticImages StaticImages => staticImages ?? throw new NullReferenceException("StaticImages not initialized");


        private static ArchonConfig? config;
        internal static ArchonConfig PluginConfig => config ?? throw new NullReferenceException("ArchonConfig not initialized");
        internal const string WorkBenchTab = "Storage";
        internal static string RootFolder { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static string ImagesFolder { get; } = Path.Combine(RootFolder, "images");

        public override string PluginId => PluginInfo.PLUGIN_GUID;

        public override string ModName => "Archon";

        public override Verbosity LogVerbosity => config?.logLevel ?? Verbosity.Verbose;

        public override void Awake()
        {
            base.Awake();
            using var log = SmartLog.For(this);
            try
            {
                log.Write($"MainPatcher.Awake()");

                Archon.GetAssets(this);

                //ModMessageSystem.SendGlobal("FindMyUpdates", "https://raw.githubusercontent.com/IronFox/Subnautica-Archon/refs/heads/main/mod-info.json");

                log.Write($"MainPatcher.Awake() done");

            }
            catch (Exception ex)
            {
                log.Error($"MainPatcher.Awake()", ex);
            }
        }


        public override void Start()
        {
            using var log = SmartLog.For(this);
            try
            {
                base.Start();
                log.Write("MainPatcher.Start()");
                LanguageHandler.RegisterLocalizationFolder();
                config = OptionsPanelHandler.RegisterModOptions<ArchonConfig>();
                var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                harmony.PatchAll();
                StartModCoroutine(
                    nameof(ArchonModController) + '.' + nameof(Register),
                    log => Register(log, Archon.staticModel!));

                log.Write("MainPatcher.Start() done");
            }
            catch (Exception ex)
            {
                log.Error("MainPatcher.Start()", ex);
            }
        }
        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.EnsureComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return (T)copy;
        }

        public Sprite? LoadSprite(string filename)
        {
            using var log = SmartLog.For(this);
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, filename);
            //Log.Write($"Trying to load sprite from {path}");
            try
            {
                return SpriteHelper.GetSpriteRaw(this, path);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to load sprite from {filename}", ex);
                return null;
            }
        }


        private IEnumerator MyRegister(SmartLog log, Archon archon, bool verbose)
        {
            var teleportNode = Node.Create("ArchonTeleportationGroup", Language.main.Get("Modules.Group.Teleportation"), SpriteHelper.RequireImage(this, "images/EmergencyTeleportationModule.png").Sprite);
            var autoAdd = EmergencyTeleportationModule.Register(this, teleportNode);
            TeleportationModuleA.RegisterAll(this, teleportNode);

            var dockingNode = Node.Create("ArchonDockingGroup", Language.main.Get("Modules.Group.Docking"), SpriteHelper.RequireImage(this, "images/DockingModuleMk1.png").Sprite);
            new DockingModule(this).Register(dockingNode);


            //Log.Write($"Loading emergency teleportation module: {autoAdd}");
            var coroutine = CraftData.GetPrefabForTechTypeAsync(autoAdd);
            yield return coroutine;
            var instance = coroutine.GetResult();
            //Log.Write($"Got module: {instance.NiceName()}");
            var pickupable = instance.SafeGetComponent<Pickupable>();
            if (pickupable.IsNull())
            {
                log.Error($"Pickupable not found on {instance.NiceName()}");
            }
            else
            {
                Archon.AutoAddEmergencyTeleport = pickupable;
            }
            yield return VehicleRegistrar.RegisterVehicle(log, this, archon, verbose);
        }


        public IEnumerator Register(SmartLog log, GameObject staticModel)
        {
            Coroutine? started = null;
            try
            {
                log.Write($"MainPatcher.Register({staticModel.NiceName()})");
                //Log.Write("model loaded: " + staticModel.name);
                var sub = staticModel.EnsureComponent<Archon>();
                //Log.Write("archon attached: " + sub.name);

                started = StartModCoroutine(
                    nameof(ArchonModController) + '.' + nameof(MyRegister),
                    log => MyRegister(log, sub, true));

                Assets.Behavior.Adapters.Log.AdapterFactory =
                    p => new LogAdapter(this, p.ForceLazy, p.Tags);

                //TorpedoModule.RegisterAll();
                //DriveModule.RegisterAll();
                //NuclearBatteryModule.RegisterAll();
                RepairModule.RegisterAll(this);






                AudioPatcher.Patcher = (source) => FreezeTimePatcher.Register(source);

                PlayerAdapter.Player = () => Player.mainObject;

                TranslationAdapter.GetTranslation = (code) =>
                {
                    return Language.main.Get($"Unity.{code}");
                };

                ActorAdapter.IsOutOfWater = (go, pos) =>
                {
                    var wf = go.GetComponent<WorldForces>();
                    return wf.IsAboveWater();
                };

                ArButtonAdapter.Instrument = (archon, arButton) =>
                {
                    using var log = SmartLog.For(this);
                    log.Write($"Instrumenting AR button {arButton.GetPath(archon.transform)}");
                    var helper = arButton.gameObject.EnsureComponent<ArchonArButton>();
                    helper.arButton = arButton;
                };

                DockingAdapter.ToDockable = (go, archonControl, filter) =>
                {
                    var v = go.GetComponent<Vehicle>();
                    if (!v)
                        return null;
                    var archon = archonControl.GetComponent<Archon>();
                    if (!archon)
                        return null;
                    if (filter == DockingAdapter.Filter.CurrentlyDockable && v.docked)
                        return null; //don't grap docked vehicles
                    if (filter == DockingAdapter.Filter.CurrentlyDockedBySaveGame && !archon.IsDockedBySavegame(go.PrefabId()))
                        return null; //don't grab vehicles that are not docked by this save game
                    var dockableMem = go.EnsureComponent<DockableMemory>();
                    if (dockableMem.Dockable.IsNull() || dockableMem.Dockable.Archon != archon)
                        dockableMem.Dockable = new DockableVehicle(v, archon);
                    return dockableMem.Dockable;
                };

                EvacuationAdapter.ShouldEvacuate = go =>
                {
                    if (go.transform.IsChildOf(Player.mainObject.transform))
                        return false;
                    var rb = go.GetComponent<Rigidbody>();
                    if (!rb || rb.isKinematic)
                        return false;
                    return true;
                };
                EvacuationAdapter.ShouldKeep = go =>
                {
                    return go == Player.mainObject;// go.transform.IsChildOf(Player.mainObject.transform);
                };

                //TargetAdapter.ResolveTarget = (go, rb) =>
                //{
                //    var mixin = go.GetComponent<LiveMixin>();
                //    if (mixin.IsNull())
                //        return null;
                //    var vehicle = go.GetComponent<Vehicle>();
                //    if (vehicle != null)
                //        return null;    //don't target vehicles
                //    if (go.name.Contains("Cyclops-MainPrefab"))
                //        return null;    //don't target cyclops
                //    return new MixinTargetAdapter(go, rb, mixin);

                //};
                //RigidbodyPatcher.Patch = (go, rb) =>
                //{
                //    try
                //    {
                //        //Log.Write($"Patching rigidbody for {go}");
                //        rb.drag = 10f;
                //        rb.angularDrag = 10f;
                //        rb.useGravity = false;
                //        //rb.interpolation = RigidbodyInterpolation.Extrapolate;
                //        //rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                //        var worldForces = CopyComponent<WorldForces>(SeamothHelper.Seamoth.GetComponent<SeaMoth>().worldForces, go);
                //        worldForces.useRigidbody = rb;
                //        worldForces.underwaterGravity = 0f;
                //        worldForces.aboveWaterGravity = 9.8f;
                //        worldForces.waterDepth = 0f;
                //        worldForces.lockInterpolation = true;

                //        //Log.Write("Rigidbody patched: " + rb);
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Write("RigidbodyAdapter.MakeRigidbody", ex);
                //        throw;
                //    }
                //};

                SoundAdapter.SoundCreator = new FModSoundCreator(this);

                log.Write("MainPatcher.Register() done");
            }
            catch (Exception ex)
            {
                log.Error($"MainPatcher.Register()", ex);
            }
            yield return started;
        }

        protected override PatcherImages LoadImages()
        {
            staticImages = new StaticImages(this);
            return staticImages;
        }
    }
}
