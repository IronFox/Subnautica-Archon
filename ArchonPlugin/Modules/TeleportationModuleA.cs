using Assets.Behavior.TransferTypes;
using AVS.Configuration;
using AVS.UpgradeModules;
using AVS.UpgradeModules.Variations;
using System.Collections.Generic;
using UnityEngine;
using AVS.Util;

namespace Subnautica_Archon.Modules
{
    internal class TeleportationModuleA : AbstractTeleportationModule
    {

        public override float EnergyCostPerSecond => 25; //~5% of all batteries full


        private TeleportationModuleA()
            : base(ArchonModule.TeleportationModuleA, TeleportationType.Normal_A)
        { }

        public static TechType Type { get; private set; } = TechType.None;
        public static TechType RegisterAll(Node node)
        {
            Type = new TeleportationModuleA().Register(node);
            autoDisplace.Add(Type);
            return Type;
        }

        private static readonly List<TechType> autoDisplace = new List<TechType>();
        public override IReadOnlyCollection<TechType>? AutoDisplace => autoDisplace;

        public override Recipe Recipe =>
            NewRecipe
                .Add(TechType.Magnetite, 4)
                .Add(TechType.ComputerChip, 2)
                .Add(TechType.Aerogel, 2)
                .Add(TechType.PrecursorIonCrystal, 1)
                .Add(TechType.PrecursorKey_Blue, 1)
                .Done();


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
                    ResetTeleportation(vehicle);

                    if (IsKeyPress(state))
                    {
                        Subtitles.Add(Language.main.Get("Modules.Teleportation.LocationRecorded"));
                        vehicle.Log.Debug($"OnToggle: Recording location");
                        vehicle.TeleportationTargetA = vehicle.transform.position;
                        vehicle.TeleportationOrientationA = vehicle.transform.rotation.eulerAngles;
                    }
                    else
                        vehicle.Log.Debug($"OnToggle: TeleportationModule1 deactivated on vehicle {vehicle.NiceName()} in slot {state.SlotID} at time {state.EventTime}. This is not a keypress");
                }
            }


        }

        protected override Vector3? GetTargetPosition(Archon vehicle) => vehicle.TeleportationTargetA;

        protected override Quaternion? GetTargetOrientation(Archon vehicle)
        {
            var euler = vehicle.TeleportationOrientationA;
            if (euler.IsNull())
                return null;
            return Quaternion.Euler(euler.Value);
        }

    }
}
