using AVS.Assets;
using AVS.Configuration;
using AVS.UpgradeModules;
using System;

namespace Subnautica_Archon.Modules
{
    public class RepairModule : ArchonModuleFamily<RepairModule>
    {
        public RepairModule(ArchonModule module)
            : base(module)
        {
        }

        public static void RegisterAll()
        {
            var node = Node.Create("ArchonRepairModules", Language.main.Get("group_RepairModule"), SpriteHelper.RequireImage("images/repairModule.png").AtlasSprite);

            new RepairModule(ArchonModule.RepairModuleMk1).Register(node);
            new RepairModule(ArchonModule.RepairModuleMk2).Register(node);
            new RepairModule(ArchonModule.RepairModuleMk3).Register(node);
        }


        public override string Description => string.Format(base.Description, Math.Round(GetRelativeSelfRepair(Module) * 100, 1));



        public static float GetRelativeSelfRepair(ArchonModule module)
        {
            switch (module)
            {
                case ArchonModule.RepairModuleMk1:
                    return 0.0025f;
                case ArchonModule.RepairModuleMk2:
                    return 0.005f;
                case ArchonModule.RepairModuleMk3:
                    return 0.01f;
            }
            return 0f;

        }

        public static ArchonModule GetFrom(Archon archon)
        {
            return archon
                .HighestModuleType(
                    ArchonModule.RepairModuleMk1,
                    ArchonModule.RepairModuleMk2,
                    ArchonModule.RepairModuleMk3);
        }


        public override Recipe Recipe
        {
            get
            {
                switch (Module)
                {
                    case ArchonModule.RepairModuleMk1:
                        return NewRecipe
                            .StartWith(TechType.Welder, 1)
                            .Include(TechType.ComputerChip, 1)
                            .Done();
                    case ArchonModule.RepairModuleMk2:
                        return NewRecipe
                            .StartWith(GetTechTypeOf(ArchonModule.RepairModuleMk1), 1)
                            .Include(TechType.Welder, 1)
                            .Include(TechType.AdvancedWiringKit, 2)
                            .Include(TechType.Magnetite, 2)
                            .Done();
                    case ArchonModule.RepairModuleMk3:
                        return NewRecipe
                            .StartWith(GetTechTypeOf(ArchonModule.RepairModuleMk2), 1)
                            .Include(TechType.Welder, 1)
                            .Include(TechType.PrecursorIonCrystal, 1)
                            .Include(TechType.Polyaniline, 2)
                            .Include(TechType.Nickel, 2)
                            .Done();
                    default:
                        return Recipe.Empty;
                }
            }
        }
    }
}
