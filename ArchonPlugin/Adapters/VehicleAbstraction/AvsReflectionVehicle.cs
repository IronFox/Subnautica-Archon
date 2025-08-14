using AVS.BaseVehicle;
using AVS.Util;
using Subnautica_Archon.Util;
using Subnautica_Archon.Util.Reflection;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class AvsReflectionVehicle
    {
        public Vehicle Vehicle { get; }

        private AvsReflectionVehicle(Vehicle vehicle)
        {
            //AvsVehicle v;
            //v.IsPlayerControlling
            Vehicle = vehicle;
            pingInstance = PropertyAdapter.OfPublic<PingInstance>(Vehicle, nameof(AvsVehicle.HudPingInstance));
            _isScuttled = FieldAdapter.Of<bool>(Vehicle, nameof(AvsVehicle.isScuttled));

            _playerExit = new MethodAdapter<bool>(Vehicle, nameof(AvsVehicle.PlayerExit));
            _playerEntry = new MethodAdapter(Vehicle, nameof(AvsVehicle.PlayerEntry));
            _beginPiloting = new MethodAdapter(Vehicle, nameof(AvsVehicle.BeginMainHelmControl));
            _onVehicleUndocked = new MethodAdapter<bool, bool>(Vehicle, nameof(AvsVehicle.UndockVehicle));
            _onVehicleDocked = new MethodAdapter<Vector3, bool>(Vehicle, nameof(AvsVehicle.DockVehicle));
            _isPlayerControlling = new MethodAdapter(Vehicle, nameof(AvsVehicle.IsPlayerControlling));
        }
        private readonly MethodAdapter<bool> _playerExit;
        private readonly MethodAdapter _playerEntry;
        private readonly MethodAdapter _beginPiloting;
        private readonly MethodAdapter<bool, bool> _onVehicleUndocked;
        private readonly MethodAdapter<Vector3, bool> _onVehicleDocked;
        private readonly PropertyAdapter<PingInstance> pingInstance;
        private readonly FieldAdapter<bool> _isScuttled;
        private readonly MethodAdapter _isPlayerControlling;

        public void PlayerExit()
        {
            _playerExit.Invoke(true);
        }

        public void PlayerEntry()
        {
            _playerEntry.Invoke();
        }

        public void BeginPiloting()
        {
            _beginPiloting.Invoke();
        }

        public void OnVehicleDocked(Vector3 exitLocation)
        {
            _onVehicleDocked.Invoke(exitLocation, false);
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
        public bool isScuttled
        {
            get => _isScuttled.Value;
            set => _isScuttled.Set(value);
        }
        public PingInstance PingInstance => pingInstance.Value.OrRequired(() => Vehicle.subName.pingInstance);

        public static bool IsOne(Vehicle vehicle)
            => vehicle.IsAvsVehicle();
        public static bool Access(Vehicle vehicle, [NotNullWhen(true)] out AvsReflectionVehicle? outVehicle)
        {
            if (!vehicle.IsAvsVehicle())
            {
                outVehicle = null;
                return false;
            }
            outVehicle = new AvsReflectionVehicle(vehicle);
            return true;
        }

        public void OnVehicleUndocked(bool boardPlayer)
            => _onVehicleUndocked.Invoke(boardPlayer, true);
    }

}