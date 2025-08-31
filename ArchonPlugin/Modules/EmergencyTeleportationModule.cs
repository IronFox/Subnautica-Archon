using Assets.Behavior.TransferTypes;
using AVS.BaseVehicle;
using AVS.Localization;
using AVS.UpgradeModules;
using AVS.UpgradeModules.Variations;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Subnautica_Archon.Modules
{
    internal class EmergencyTeleportationModule : AbstractTeleportationModule
    {
        private EmergencyTeleportationModule(ArchonModController mp)
            : base(mp, ArchonModule.EmergencyTeleportationModule, TeleportationType.Emergency)
        { }



        public static TechType Type { get; private set; } = TechType.None;

        //(647.0, -19.1, 381.9)
        //@(811.5, -19.2, 350.5)
        public static TechType Register(ArchonModController mp, Node node)
        {
            Type = new EmergencyTeleportationModule(mp).Register(node);
            return Type;
        }



        protected override void OnToggle(IToggleState state)
        {
            base.OnToggle(state);
            if (!state.IsActive)
            {
                var vehicle = state.Vehicle as Archon;
                if (vehicle != null)
                {
                    base.ResetTeleportation(vehicle);
                }
                //ErrorMessage.AddError(Language.main.Get("Modules.EmergencyTeleportation.Aborted"));
            }
        }

        public override bool CanRemoveFrom(AvsVehicle vehicle, [NotNullWhen(false)] out MaybeTranslate? errorMessage)
        {
            var count = this.TechTypes.CountSumIn(vehicle.modules);
            if (count > 1)
            {
                errorMessage = null;
                return true;
            }
            errorMessage = AVS.Localization.Text.Translated($"Error.EmergencyTeleportationModule.CannotRemoveLast");
            return false;
        }

        protected override Vector3? GetTargetPosition(Archon vehicle) => new Vector3(514.9f, -20f, 311.0f);

        protected override Quaternion? GetTargetOrientation(Archon vehicle) => Quaternion.identity;

    }
}
