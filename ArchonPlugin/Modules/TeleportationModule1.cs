using Assets.Behavior.TransferTypes;
using AVS.Assets;
using AVS.Crafting;
using AVS.UpgradeModules;
using AVS.UpgradeModules.Variations;
using Subnautica_Archon.Util;
using UnityEngine;

namespace Subnautica_Archon.Modules
{
    internal class TeleportationModule1 : ToggleableModule
    {
        public static float SecondsUntilTeleport { get; } = 5f;


        public override string ClassId => $"ArchonTeleportationModule1";

        public override string DisplayName => Language.main.Get("Modules.Teleportation1.Name");

        public override string Description => Language.main.Get("Modules.Teleportation1.Description");

        public override float EnergyCostPerActivation => 10;

        public override Atlas.Sprite Icon => icon!.Value.AtlasSprite;

        private static Image? icon;
        public static TechType Register(Node node)
        {
            icon = SpriteHelper.RequireImage("images/TeleportationModule1.png");
            var instance = new TeleportationModule1();
            return node.RegisterUpgrade(instance, UpgradeCompat.AvsVehiclesOnly).ForAvsVehicle;
        }


        protected override void OnRepeat(IToggleState state)
        {
            base.OnRepeat(state);
            var vehicle = state.Vehicle as Archon;
            if (vehicle == null)
                return;
            var target = vehicle.TeleportationTarget1;
            var orientation = vehicle.TeleportationOrientation1;
            if (target == null || orientation == null)
            {
                vehicle.Log.Debug($"TeleportationModule1.OnRepeat: No target or orientation set for vehicle {vehicle.NiceName()} in slot {state.SlotID} at time {state.EventTime}");
                return;
            }


            try
            {
                float remainingTime = SecondsUntilTeleport - state.EventTime;
                float lastRemainingTime = SecondsUntilTeleport - state.LastRepeatTime;
                //Log.Write($"EmergencyTeleportationModule.OnRepeat(vehicle={param.Vehicle},remainingTime={remainingTime},lastRemainingTime={lastRemainingTime})");
                if (remainingTime > 0)
                {
                    vehicle.Control.secondsToTeleport = remainingTime;
                    vehicle.Control.teleportationProgress = 1f - (remainingTime / SecondsUntilTeleport);
                    vehicle.Control.teleportationType = TeleportationType.Normal1;
                    if (Mathf.RoundToInt(remainingTime) != Mathf.RoundToInt(lastRemainingTime))
                    {
                        Subtitles.Add(Language.main.GetFormat($"Modules.Teleportation.ActivatingInSeconds", Mathf.RoundToInt(remainingTime)));
                    }
                }
                else
                {
                    Log.Write($"TeleportationModule1.OnRepeat: Teleportation finished, teleporting vehicle {state.Vehicle} to {target}");

                    Subtitles.Add(Language.main.Get("Modules.Teleportation.ActivatingNow"));
                    vehicle.Engine.KillMomentum();
                    vehicle.transform.position = target.Value;
                    vehicle.transform.rotation = Quaternion.Euler(orientation.Value);
                    vehicle.Control.SignalTeleported();
                    vehicle.Control.secondsToTeleport = 100;
                    vehicle.Control.teleportationProgress = 0;
                    vehicle.Control.teleportationType = TeleportationType.None;

                    state.Deactivate();
                }
            }
            catch (System.Exception ex)
            {
                Log.Exception($"TeleportationModule1.OnRepeat: Exception occurred", ex);
                state.Deactivate();
            }
        }
        private bool IsKeyPress(IToggleState state)
            => state.EventTime > 0.05f && state.EventTime < 0.5f;

        protected override void OnToggle(IToggleState state)
        {
            base.OnToggle(state);
            var vehicle = state.Vehicle as Archon;
            if (vehicle != null)
            {
                if (!state.IsActive)
                {
                    vehicle.Control.secondsToTeleport = 100;
                    vehicle.Control.teleportationProgress = 0;
                    vehicle.Control.teleportationType = TeleportationType.None;

                    if (IsKeyPress(state))
                    {
                        Subtitles.Add(Language.main.Get("Modules.Teleportation.Recorded"));
                        vehicle.Log.Debug($"OnToggle: Recording location");
                        vehicle.TeleportationTarget1 = vehicle.transform.position;
                        vehicle.TeleportationOrientation1 = vehicle.transform.rotation.eulerAngles;
                    }
                    else
                        vehicle.Log.Debug($"OnToggle: TeleportationModule1 deactivated on vehicle {vehicle.NiceName()} in slot {state.SlotID} at time {state.EventTime}. This is not a keypress");
                }
            }


        }
    }
}
