using System.Reflection;
using UnityEngine;

namespace Subnautica_Archon.Util
{
    internal class VFVehicle
    {
        public Vehicle Vehicle { get; }

        private VFVehicle(Vehicle vehicle)
        {
            Vehicle = vehicle;
            hudPingInstance = Vehicle.GetType().GetProperty("HudPingInstance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        private readonly SimpleMethodHelper<Void> _playerExit
            = new SimpleMethodHelper<Void>("PlayerExit");
        private readonly SimpleMethodHelper<Void> _playerEntry
            = new SimpleMethodHelper<Void>("PlayerEntry");
        private readonly SimpleMethodHelper<Void> _beginPiloting
            = new SimpleMethodHelper<Void>("BeginPiloting");
        private readonly SimpleMethodHelper<Void> _onVehicleUndocked
            = new SimpleMethodHelper<Void>("OnVehicleUndocked");
        private MethodAdapter<Vector3>? _onVehicleDocked;
        private readonly PropertyInfo hudPingInstance;

        public void PlayerExit()
        {
            _playerExit.ExecuteOn(Vehicle);
        }

        public void PlayerEntry()
        {
            _playerEntry.ExecuteOn(Vehicle);
        }

        public void BeginPiloting()
        {
            _beginPiloting.ExecuteOn(Vehicle);
        }

        public void OnVehicleDocked(Vector3 exitLocation)
        {
            if (_onVehicleDocked is null)
            {
                _onVehicleDocked = new MethodAdapter<Vector3>(Vehicle, "OnVehicleDocked");
            }
            _onVehicleDocked.Invoke(exitLocation);
        }

        public void SetHudIcon(bool visible)
        {
            if (hudPingInstance is null)
            {
                Log.Error("HudPingInstance property not found on Vehicle");
                return;
            }
            var pingInstance = hudPingInstance.GetValue(Vehicle) as PingInstance;
            if (pingInstance is null)
            {
                Log.Error("HudPingInstance is not a PingInstance");
                return;
            }
            pingInstance.SetVisible(visible);
            pingInstance.enabled = visible;
        }

        public static bool IsOne(Vehicle vehicle)
            => ObjectHelper.IsVFVehicle(vehicle);
        public static bool Access(Vehicle vehicle, out VFVehicle? outVehicle)
        {
            if (!ObjectHelper.IsVFVehicle(vehicle))
            {
                outVehicle = null;
                return false;
            }
            outVehicle = new VFVehicle(vehicle);
            return true;
        }

        public void OnVehicleUndocked()
            => _onVehicleUndocked.ExecuteOn(Vehicle);
    }
}
