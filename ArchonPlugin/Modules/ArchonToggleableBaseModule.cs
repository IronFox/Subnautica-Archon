using AVS;
using AVS.Crafting;
using AVS.Log;
using AVS.UpgradeModules;
using AVS.UpgradeModules.Variations;
using AVS.Util;
using Subnautica_Archon.Exceptions;
using Subnautica_Archon.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Subnautica_Archon.Modules
{
    public abstract class ArchonToggleableBaseModule : ToggleableModule
    {
        public ArchonModule Module { get; }
        private Sprite? icon;



        public override string ClassId => $"Archon{Module}";

        public override string Description => Language.main.Get("Modules." + Module + ".Description");
        public override string DisplayName => Language.main.Get("Modules." + Module + ".Name");

        //public static CraftingNode RootCraftingNode { get; } = new CraftingNode
        //(
        //    displayName: $"Archon",
        //    icon: Archon.craftingSprite!,
        //    name: $"archonupgradetab"
        //);

        public static string GetMarkFromType(ArchonModule m)
        {
            var s = m.ToString();
            return s.Substring(s.Length - 3);

        }

        public string MarkFromType => GetMarkFromType(Module);

        public ArchonToggleableBaseModule(ArchonModController mp, ArchonModule module)
        {
            Module = module;
            Owner = mp;
            var path = $"images/{module}.png";
            icon = mp.LoadSprite(path);
            if (icon.IsNull())
                throw new InitializationException($"Error while constructing {module} {this}: File {path} not found");
        }
        public override AVS.RootModController Owner { get; }

        public virtual TechType Register(Node node)
        {
            var compat = UpgradeCompat.AvsVehiclesOnly;

            var type = node.RegisterUpgrade(this, compat).ForAvsVehicle;
            All[type] = this;
            AllReverse[Module] = type;

            //Log.Debug($"Registered module {Module} {this} as tech type {type}");

            return type;
        }

        private static Dictionary<TechType, ArchonToggleableBaseModule> All { get; } = new Dictionary<TechType, ArchonToggleableBaseModule>();
        private static Dictionary<ArchonModule, TechType> AllReverse { get; } = new Dictionary<ArchonModule, TechType>();
        public static IReadOnlyDictionary<TechType, ArchonToggleableBaseModule> Registered => All;
        public static IReadOnlyDictionary<ArchonModule, TechType> TechTypeMap => AllReverse;

        public static TechType GetTechTypeOf(RootModController rmc, ArchonModule module)
        {
            if (TechTypeMap.TryGetValue(module, out var type))
                return type;
            using var log = SmartLog.LazyFor(rmc);
            log.Error($"Unable to retrieve tech type of archon module {module}: not registered");
            return TechType.None;
        }

        public override bool IsVehicleSpecific => true;
        public override void OnAdded(AddActionParams param)
        {
            using var log = SmartLog.For(Owner);
            base.OnAdded(param);
            var now = DateTime.Now;

            log.Write($"ArchonBaseModule[{Module}].OnAdded(vehicle={param.Vehicle.NiceName()},isAdded={param.Added},slot={param.SlotID})");
            var archon = param.Vehicle as Archon;
            if (archon.IsNull())
            {
                log.Error($"Added to incompatible vehicle {param.Vehicle.NiceName()}");
                ErrorMessage.AddWarning("This is an Archon upgrade and will not work on other subs!");
                return;
            }

            var cnt = GetNumberInstalled(archon);
            archon.SetModuleCount(Module, cnt);
        }
        public override void OnRemoved(AddActionParams param)
        {
            var archon = param.Vehicle as Archon;
            if (archon.IsNull())
            {
                return;
            }
            archon.SetModuleCount(Module, GetNumberInstalled(archon));
        }

        public override Sprite Icon => icon ?? throw new InitializationException("Module Icon should have been loaded by now");

    }
}
