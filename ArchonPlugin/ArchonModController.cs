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
                log.Write($"ArchonModController.Awake()");

                Archon.GetAssets(this);

                //ModMessageSystem.SendGlobal("FindMyUpdates", "https://raw.githubusercontent.com/IronFox/Subnautica-Archon/refs/heads/main/mod-info.json");

                log.Write($"ArchonModController.Awake() done");

            }
            catch (Exception ex)
            {
                log.Error($"ArchonModController.Awake()", ex);
            }
        }

        private void DefineAdapters()
        {
            using var log = SmartLog.For(this);
            try
            {
                Assets.Behavior.Adapters.Log.AdapterFactory =
                    p => new LogAdapter(this, p.ForceLazy, p.Tags);
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
                    using var log = new SmartLog(this, Domain.Mod, 0, parameters: Params.Of(archon, arButton), nameOverride: "ArButtonAdapter.Instrument");
                    log.Debug($"Instrumenting AR button {arButton.GetPath(archon.transform)}");
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

                SoundAdapter.SoundCreator = new FModSoundCreator(this);
            }
            catch (Exception ex)
            {
                log.Error($"ArchonModController.DefineAdapters()", ex);
            }
        }

        private TechType RegisterModules()
        {
            using var log = SmartLog.For(this);
            try
            {
                log.Write("Registering Archon modules");

                var teleportNode = Node.Create("ArchonTeleportationGroup", Language.main.Get("Modules.Group.Teleportation"), SpriteHelper.RequireImage(this, "images/EmergencyTeleportationModule.png").Sprite);
                var autoAdd = EmergencyTeleportationModule.Register(this, teleportNode);
                TeleportationModuleA.RegisterAll(this, teleportNode);

                var dockingNode = Node.Create("ArchonDockingGroup", Language.main.Get("Modules.Group.Docking"), SpriteHelper.RequireImage(this, "images/DockingModuleMk1.png").Sprite);
                new DockingModule(this).Register(dockingNode); log.Write("Archon modules registered");

                return autoAdd;
            }
            catch (Exception ex)
            {
                log.Error($"ArchonModController.RegisterModules()", ex);
                return TechType.None;
            }
        }


        public override void Start()
        {
            using var log = SmartLog.For(this);
            try
            {
                base.Start();
                log.Write("ArchonModController.Start()");
                LanguageHandler.RegisterLocalizationFolder();
                config = OptionsPanelHandler.RegisterModOptions<ArchonConfig>();
                var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                harmony.PatchAll();

                DefineAdapters();
                var sub = Archon.staticModel!;
                var arc = sub.EnsureComponent<Archon>();

                var autoAdd = RegisterModules();

                StartModCoroutine(
                    nameof(ArchonModController) + '.' + nameof(Register),
                    log => Register(log, arc, autoAdd, PluginConfig.logLevel == Verbosity.Verbose));

                log.Write("ArchonModController.Start() done");
            }
            catch (Exception ex)
            {
                log.Error("ArchonModController.Start()", ex);
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


        private IEnumerator Register(SmartLog log, Archon archon, TechType autoAdd, bool verbose)
        {

            GameObject? instance = null;

            if (autoAdd != TechType.None)
            {
                //Log.Write($"Loading emergency teleportation module: {autoAdd}");
                var coroutine = CraftData.GetPrefabForTechTypeAsync(autoAdd);
                yield return coroutine;
                instance = coroutine.GetResult();
            }

            //Log.Write($"Got module: {instance.NiceName()}");
            var pickupable = instance.SafeGetComponent<Pickupable>();
            if (pickupable.IsNull())
            {
                if (autoAdd != TechType.None)
                    log.Error($"Pickupable not found on {instance.NiceName()}");
            }
            else
            {
                Archon.AutoAddEmergencyTeleport = pickupable;
            }
            yield return VehicleRegistrar.RegisterVehicle(log, this, archon, verbose);
        }


        protected override PatcherImages LoadImages()
        {
            staticImages = new StaticImages(this);
            return staticImages;
        }
    }
}
