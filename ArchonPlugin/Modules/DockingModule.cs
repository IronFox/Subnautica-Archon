using AVS.BaseVehicle;
using AVS.Localization;
using AVS.UpgradeModules;
using System.Diagnostics.CodeAnalysis;

namespace Subnautica_Archon.Modules
{
    public class DockingModule : ArchonBaseModule
    {
        public DockingModule() : base(ArchonModule.DockingModuleMk1)
        {
        }



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
