﻿using System;
using System.Collections.Generic;
using VehicleFramework.UpgradeTypes;

namespace Subnautica_Archon.Modules
{
    public abstract class ArchonModuleFamily<T> : ArchonBaseModule
        where T : ArchonModuleFamily<T>
    {
        protected ArchonModuleFamily(ArchonModule module, CraftingNode groupNode) : base(module, groupNode)
        {
        }
        private static Dictionary<TechType, T> Family { get; } = new Dictionary<TechType, T>();
        public static IReadOnlyDictionary<TechType, T> RegisteredFamily => Family;

        public override IReadOnlyCollection<TechType> AutoDisplace => Family.Keys;

        public override TechType Register()
        {
            var type = base.Register();

            Family[type] = (T)this;
            return type;
        }

        public static TechType FindRegisteredFamilyMemberTechType(Func<T, bool> predicate)
        {
            foreach (var family in RegisteredFamily)
                if (predicate(family.Value))
                    return family.Key;
            return TechType.None;
        }
    }
}