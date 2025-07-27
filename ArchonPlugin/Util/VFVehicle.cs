using UnityEngine;

namespace Subnautica_Archon.Util
{
    internal class VFVehicle
    {
        public Vehicle Vehicle { get; }

        private VFVehicle(Vehicle vehicle)
        {
            Vehicle = vehicle;
            pingInstance = FieldAdapter.Of<PingInstance>(Vehicle, "pingInstance");
        }
        private readonly SimpleMethodHelper<Void> _playerExit
            = new SimpleMethodHelper<Void>("PlayerExit");
        private readonly SimpleMethodHelper<Void> _playerEntry
            = new SimpleMethodHelper<Void>("PlayerEntry");
        private readonly SimpleMethodHelper<Void> _beginPiloting
            = new SimpleMethodHelper<Void>("BeginPiloting");
        private readonly SimpleMethodHelper<Void> _onVehicleUndocked
            = new SimpleMethodHelper<Void>("OnVehicleUndocked");
        private MethodAdapter<Vehicle, Vector3>? _onVehicleDocked0;
        private MethodAdapter<Vector3>? _onVehicleDocked1;
        private readonly FieldAdapter<PingInstance> pingInstance;

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
            if (_onVehicleDocked0 is null)
                _onVehicleDocked0 = new MethodAdapter<Vehicle, Vector3>(Vehicle, "OnVehicleDocked", ignoreMissing: true);
            if (_onVehicleDocked1 is null)
                _onVehicleDocked1 = new MethodAdapter<Vector3>(Vehicle, "OnVehicleDocked", ignoreMissing: true);
            if (_onVehicleDocked0 != null)
                _onVehicleDocked0.Invoke(Vehicle, exitLocation);
            else if (_onVehicleDocked1 != null)
                _onVehicleDocked1.Invoke(exitLocation);
            else
                Log.Error("OnVehicleDocked method not found on Vehicle");
        }

        public bool HudIconIsEnabled()
        {
            var pi = pingInstance.Value;
            if (pi is null)
            {
                Log.Error("pingInstance not set on " + Vehicle.NiceName());
                return false;
            }
            return pi.enabled || pi.visible;
        }

        public void SetHudIcon(bool visible)
        {

            var pi = pingInstance.Value;
            if (pi is null)
            {
                Log.Error("pingInstance not set on " + Vehicle.NiceName());
                return;
            }
            pi.SetVisible(visible);
            pi.enabled = visible;

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
