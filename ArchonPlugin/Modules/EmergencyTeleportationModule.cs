using Assets.Behavior.TransferTypes;
using AVS.Assets;
using AVS.BaseVehicle;
using AVS.Crafting;
using AVS.Localization;
using AVS.UpgradeModules;
using AVS.UpgradeModules.Variations;
using Subnautica_Archon.Util;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Subnautica_Archon.Modules
{
    internal class EmergencyTeleportationModule : ToggleableModule
    {
        public static float SecondsUntilTeleport { get; } = 5f;


        public override string ClassId => $"ArchonEmergencyTeleportationModule";

        public override string DisplayName => Language.main.Get("Modules.EmergencyTeleportation.Name");

        public override string Description => Language.main.Get("Modules.EmergencyTeleportation.Description");

        public override Atlas.Sprite Icon => icon!.Value.AtlasSprite;

        private static Image? icon;

        //(647.0, -19.1, 381.9)
        //@(811.5, -19.2, 350.5)
        public static TechType Register(Node node)
        {
            icon = SpriteHelper.RequireImage("images/EmergencyTeleportationModule.png");
            var instance = new EmergencyTeleportationModule();
            return node.RegisterUpgrade(instance, UpgradeCompat.AvsVehiclesOnly).ForAvsVehicle;
        }


        protected override void OnRepeat(IToggleState state)
        {
            base.OnRepeat(state);
            try
            {
                float remainingTime = SecondsUntilTeleport - state.EventTime;
                float lastRemainingTime = SecondsUntilTeleport - state.LastRepeatTime;
                //Log.Write($"EmergencyTeleportationModule.OnRepeat(vehicle={param.Vehicle},remainingTime={remainingTime},lastRemainingTime={lastRemainingTime})");
                if (remainingTime > 0)
                {
                    var vehicle = state.Vehicle as Archon;
                    if (vehicle != null)
                    {
                        vehicle.Control.secondsToTeleport = remainingTime;
                        vehicle.Control.teleportationProgress = 1f - (remainingTime / SecondsUntilTeleport);
                        vehicle.Control.teleportationType = TeleportationType.Emergency;
                    }
                    if (Mathf.RoundToInt(remainingTime) != Mathf.RoundToInt(lastRemainingTime))
                    {
                        Subtitles.Add(Language.main.GetFormat($"Modules.Teleportation.ActivatingInSeconds", Mathf.RoundToInt(remainingTime)));
                    }
                }
                else
                {
                    Log.Write($"EmergencyTeleportationModule.OnRepeat: Teleportation finished, teleporting vehicle {state.Vehicle}");
                    Subtitles.Add(Language.main.Get("Modules.Teleportation.ActivatingNow"));
                    var vehicle = state.Vehicle as Archon;
                    if (vehicle != null)
                    {
                        vehicle.Engine.KillMomentum();
                        vehicle.TeleportVehicle(new Vector3(514.9f, -20f, 311.0f), Quaternion.identity);
                        //vehicle.transform.position = new Vector3(514.9f, -20f, 311.0f);
                        //vehicle.transform.rotation = Quaternion.identity;
                        vehicle.Control.SignalTeleported();
                        vehicle.Control.secondsToTeleport = 100;
                        vehicle.Control.teleportationProgress = 0;
                        vehicle.Control.teleportationType = TeleportationType.None;
                    }
                    else
                    {
                        ErrorMessage.AddError(Language.main.Get("Modules.Teleportation.Failed"));
                    }
                    state.Deactivate();
                }
            }
            catch (System.Exception ex)
            {
                Log.Exception($"EmergencyTeleportationModule.OnRepeat: Exception occurred", ex);
                state.Deactivate();
            }
        }

        protected override void OnToggle(IToggleState state)
        {
            base.OnToggle(state);
            if (!state.IsActive)
            {
                var vehicle = state.Vehicle as Archon;
                if (vehicle != null)
                {
                    vehicle.Control.secondsToTeleport = 100;
                    vehicle.Control.teleportationProgress = 0;
                    vehicle.Control.teleportationType = TeleportationType.None;
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
    }
}
