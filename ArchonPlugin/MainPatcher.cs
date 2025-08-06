using AVS;
using AVS.Assets;
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
    public class MainPatcher : AVS.MainPatcher
    {
        private static StaticImages? staticImages;
        public static StaticImages StaticImages => staticImages ?? throw new NullReferenceException("StaticImages not initialized");


        private static ArchonConfig? config;
        internal static ArchonConfig PluginConfig => config ?? throw new NullReferenceException("ArchonConfig not initialized");
        internal const string WorkBenchTab = "Storage";
        internal static string RootFolder { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static string ImagesFolder { get; } = Path.Combine(RootFolder, "images");

        public override string PluginId => PluginInfo.PLUGIN_GUID;


        public override void Awake()
        {
            try
            {
                base.Awake();
                Log.Write($"MainPatcher.Awake()");

                Archon.GetAssets();
                Log.Write($"MainPatcher.Awake() done");

            }
            catch (Exception ex)
            {
                Log.Write($"MainPatcher.Awake()", ex);
            }
        }


        public override void Start()
        {
            try
            {
                base.Start();
                Log.Write("MainPatcher.Start()");
                LanguageHandler.RegisterLocalizationFolder();
                config = OptionsPanelHandler.RegisterModOptions<ArchonConfig>();
                var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                harmony.PatchAll();
                UWE.CoroutineHost.StartCoroutine(Register(Archon.staticModel!));

                Log.Write("MainPatcher.Start() done");
            }
            catch (Exception ex)
            {
                Log.Write("MainPatcher.Start()", ex);
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

        public static Atlas.Sprite? LoadSprite(string filename)
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), filename);
            Log.Write($"Trying to load sprite from {path}");
            try
            {
                return SpriteHelper.GetSprite(path);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                return null;
            }
        }


        private IEnumerator MyRegister(Archon archon, bool verbose)
        {


            var teleportNode = Node.Create("ArchonTeleportationGroup", Language.main.Get("Modules.Group.Teleportation"), SpriteHelper.RequireImage("images/EmergencyTeleportationModule.png").AtlasSprite);
            var autoAdd = EmergencyTeleportationModule.Register(teleportNode);
            TeleportationModule1.Register(teleportNode);

            var dockingNode = Node.Create("ArchonDockingGroup", Language.main.Get("Modules.Group.Docking"), SpriteHelper.RequireImage("images/DockingModuleMk1.png").AtlasSprite);
            new DockingModule().Register(dockingNode);


            Log.Write($"Loading emergency teleportation module: {autoAdd}");
            var coroutine = CraftData.GetPrefabForTechTypeAsync(autoAdd);
            yield return coroutine;
            var instance = coroutine.GetResult();
            Log.Write($"Got module: {instance.NiceName()}");
            var pickupable = instance.SafeGetComponent<Pickupable>();
            if (pickupable == null)
            {
                Log.Error($"Pickupable not found on {instance.NiceName()}");
            }
            else
            {
                archon.whenReadySlotInstanceOf = pickupable;
            }
            yield return VehicleRegistrar.RegisterVehicle(archon, verbose);
        }


        public IEnumerator Register(GameObject staticModel)
        {
            Coroutine? started = null;
            try
            {
                Log.Write("MainPatcher.Register()");
                Log.Write("model loaded: " + staticModel.name);
                var sub = staticModel.EnsureComponent<Archon>();
                Log.Write("archon attached: " + sub.name);

                started = UWE.CoroutineHost.StartCoroutine(MyRegister(sub, true));

                Assets.Behavior.Adapters.Log.AdapterFactory =
                    tags => new LogAdapter(tags);

                //TorpedoModule.RegisterAll();
                //DriveModule.RegisterAll();
                //NuclearBatteryModule.RegisterAll();
                RepairModule.RegisterAll();






                AudioPatcher.Patcher = (source) => FreezeTimePatcher.Register(source);

                PlayerAdapter.Player = () => Player.mainObject;

                ActorAdapter.IsOutOfWater = (go, pos) =>
                {
                    var wf = go.GetComponent<WorldForces>();
                    return wf.IsAboveWater();
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
                    var dmem = go.EnsureComponent<DockableMemory>();
                    dmem.Dockable = dmem.Dockable ?? new DockableVehicle(v, archon);
                    return dmem.Dockable;
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
                //    if (mixin == null)
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

                SoundAdapter.SoundCreator = new FModSoundCreator();

                Log.Write("MainPatcher.Register() done");
            }
            catch (Exception ex)
            {
                Log.Write($"MainPatcher.Register()", ex);
            }
            yield return started;
        }

        protected override PatcherImages LoadImages()
        {
            staticImages = new StaticImages();
            return staticImages;
        }
    }
}
