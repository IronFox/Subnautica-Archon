using AVS.UpgradeModules;
using System;
using System.Collections.Generic;


namespace Subnautica_Archon.Modules
{
    public abstract class ArchonModuleFamily<T> : ArchonBaseModule
        where T : ArchonModuleFamily<T>
    {
        protected ArchonModuleFamily(ArchonModule module) : base(module)
        {
        }
        private static Dictionary<TechType, T> Family { get; } = new Dictionary<TechType, T>();
        public static IReadOnlyDictionary<TechType, T> RegisteredFamily => Family;

        public override IReadOnlyCollection<TechType> AutoDisplace => Family.Keys;

        public override TechType Register(Node node)
        {
            var type = base.Register(node);

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