using AVS.BaseVehicle;
using AVS.Configuration;
using AVS.Localization;
using AVS.UpgradeModules;
using System.Diagnostics.CodeAnalysis;

namespace Subnautica_Archon.Modules
{
    public class DockingModule : ArchonBaseModule
    {
        public DockingModule(ArchonModController mp) : base(mp, ArchonModule.DockingModuleMk1)
        { }

        public override Recipe Recipe =>
            NewRecipe
                .Add(TechType.PlasteelIngot, 2)
                .Add(TechType.ComputerChip, 3)
                .Add(TechType.Magnetite, 2)
                .Add(TechType.Polyaniline, 2)
                .Add(TechType.PrecursorIonCrystal, 1)
                .Done();

        public override void OnAdded(AddActionParams param)
        {
            base.OnAdded(param);
            if (param.Vehicle is Archon archon)
            {
                archon.SetModuleCount(Module, GetNumberInstalled(archon));
            }
        }

        public override void OnRemoved(AddActionParams param)
        {
            base.OnRemoved(param);
            if (param.Vehicle is Archon archon)
            {
                archon.SetModuleCount(Module, GetNumberInstalled(archon));
            }
        }

        public override bool CanRemoveFrom(AvsVehicle vehicle, [NotNullWhen(false)] out MaybeTranslate? errorMessage)
        {
            if (!base.CanRemoveFrom(vehicle, out errorMessage))
                return false;
            if (!(vehicle is Archon archon))
                return true;

            if (archon.Control.bayControl.NumDockedVehicles > archon.GetTotalDockingCapacityWithOneLess(Module))
            {
                errorMessage = Text.Translated("Modules.DockingModule.CannotRemoveOccupied");
                return false;
            }
            return true;
        }
    }
}
