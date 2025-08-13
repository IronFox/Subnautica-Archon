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
            var node = Node.Create("ArchonRepairModules", Language.main.Get("Modules.Group.RepairModule"), SpriteHelper.RequireImage("images/repairModule.png").Sprite);

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
                            .Add(TechType.Welder, 1)
                            .Add(TechType.ComputerChip, 1)
                            .Done();
                    case ArchonModule.RepairModuleMk2:
                        return NewRecipe
                            .Add(GetTechTypeOf(ArchonModule.RepairModuleMk1), 1)
                            .Add(TechType.Welder, 1)
                            .Add(TechType.AdvancedWiringKit, 2)
                            .Add(TechType.Magnetite, 2)
                            .Done();
                    case ArchonModule.RepairModuleMk3:
                        return NewRecipe
                            .Add(GetTechTypeOf(ArchonModule.RepairModuleMk2), 1)
                            .Add(TechType.Welder, 1)
                            .Add(TechType.PrecursorIonCrystal, 1)
                            .Add(TechType.Polyaniline, 2)
                            .Add(TechType.Nickel, 2)
                            .Done();
                    default:
                        return Recipe.Empty;
                }
            }
        }
    }
}
