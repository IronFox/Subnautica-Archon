using AVS.Assets;
using AVS.BaseVehicle;
using AVS.Crafting;
using AVS.Localization;
using AVS.UpgradeModules;
using System.Diagnostics.CodeAnalysis;

namespace Subnautica_Archon.Modules
{
    internal class EmergencyTeleportationModule : ToggleableUpgrade
    {
        public override string ClassId => $"ArchonEmergencyTeleportationModule";

        public override string DisplayName => Language.main.Get("display_ArchonEmergencyTeleportationModule");

        public override string Description => Language.main.Get("desc_ArchonEmergencyTeleportationModule");

        public override Atlas.Sprite Icon => icon!.Value.AtlasSprite;

        private static Image? icon;

        public static TechType Register(Node node)
        {
            icon = SpriteHelper.RequireImage("images/EmergencyTeleportationModule.png");
            var instance = new EmergencyTeleportationModule();
            return node.RegisterUpgrade(instance, UpgradeCompat.AvsVehiclesOnly).ForAvsVehicle;
        }

        public override bool CanRemoveFrom(AvsVehicle vehicle, [NotNullWhen(false)] out MaybeTranslate? errorMessage)
        {
            var count = this.TechTypes.CountSumIn(vehicle.modules);
            if (count > 1)
            {
                errorMessage = null;
                return true;
            }
            errorMessage = Text.Translated($"error_ArchonEmergencyTeleportationModule_CannotRemoveLast");
            return false;
        }
    }
}
