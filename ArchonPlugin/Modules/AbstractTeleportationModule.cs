using Assets.Behavior.TransferTypes;
using AVS;
using AVS.UpgradeModules.Variations;
using UnityEngine;

namespace Subnautica_Archon.Modules
{
    public abstract class AbstractTeleportationModule : ArchonToggleableBaseModule
    {
        public static float SecondsUntilTeleport { get; } = 5f;
        public TeleportationType TeleportationType { get; }

        protected AbstractTeleportationModule(ArchonModule module, TeleportationType type) : base(module)
        {
            TeleportationType = type;
        }


        protected abstract Vector3? GetTargetPosition(Archon vehicle);
        protected abstract Quaternion? GetTargetOrientation(Archon vehicle);

        protected override void OnRepeat(IToggleState state)
        {
            base.OnRepeat(state);
            var vehicle = state.Vehicle as Archon;
            if (vehicle == null)
                return;
            var target = GetTargetPosition(vehicle);
            var orientation = GetTargetOrientation(vehicle);
            if (target == null || orientation == null)
            {
                vehicle.Log.Debug($"{Module}.OnRepeat: No target or orientation set for vehicle {vehicle.NiceName()} in slot {state.SlotID} at time {state.EventTime}");
                return;
            }
            var voiceLibrary = vehicle.GetVoiceLibrary();


            try
            {
                if (state.RepeatIteration == 0)
                {
                    var voice = voiceLibrary?.GetRandomPrepareTeleport();
                    vehicle.VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Teleportation.Start", 0));
                }
                float remainingTime = SecondsUntilTeleport - state.EventTime;
                float lastRemainingTime = SecondsUntilTeleport - state.LastRepeatTime;
                //Log.Write($"EmergencyTeleportationModule.OnRepeat(vehicle={param.Vehicle},remainingTime={remainingTime},lastRemainingTime={lastRemainingTime})");
                if (remainingTime > 0)
                {
                    vehicle.Control.secondsToTeleport = remainingTime;
                    vehicle.Control.teleportationProgress = 1f - (remainingTime / SecondsUntilTeleport);
                    vehicle.Control.teleportationType = TeleportationType;
                    //if (Mathf.RoundToInt(remainingTime) != Mathf.RoundToInt(lastRemainingTime))
                    //{
                    //    if (Mathf.RoundToInt(remainingTime) > 1)
                    //        Subtitles.Add(Language.main.GetFormat($"Modules.Teleportation.ActivatingInSeconds", Mathf.RoundToInt(remainingTime)));
                    //}
                }
                else
                {
                    vehicle.Log.Write($"TeleportationModule1.OnRepeat: Teleportation finished, teleporting vehicle {state.Vehicle} to {target}");

                    var voice = voiceLibrary?.GetRandomTeleport();
                    vehicle.VoiceQueue.Play(new VoiceLine(voice, "Subtitle.Voice.Teleportation.ActivatingNow", 0));

                    vehicle.Engine.KillMomentum();
                    vehicle.TeleportVehicle(target.Value, orientation.Value);
                    //vehicle.transform.position = target.Value;
                    //vehicle.transform.rotation = Quaternion.Euler(orientation.Value);
                    vehicle.Control.SignalTeleported();
                    vehicle.Control.secondsToTeleport = 100;
                    vehicle.Control.teleportationProgress = 0;
                    vehicle.Control.teleportationType = TeleportationType.None;

                    state.Deactivate();
                }
            }
            catch (System.Exception ex)
            {
                vehicle.Log.Error($"TeleportationModule1.OnRepeat: Exception occurred", ex);
                state.Deactivate();
            }
        }

        protected void ResetTeleportation(Archon vehicle)
        {
            vehicle.Control.secondsToTeleport = 100;
            vehicle.Control.teleportationProgress = 0;
            vehicle.Control.teleportationType = TeleportationType.None;

        }
    }
}