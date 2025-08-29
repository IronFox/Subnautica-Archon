using AVS;
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
        public RootModController Rmc { get; }
        public Vehicle Vehicle { get; }

        private AvsReflectionVehicle(RootModController rmc, Vehicle vehicle)
        {
            Rmc = rmc;
            //AvsVehicle v;
            //v.IsPlayerControlling
            Vehicle = vehicle;

            pingInstance = PropertyAdapter.OfPublic<PingInstance>(Vehicle, nameof(AvsVehicle.HudPingInstance));
            _isScuttled = FieldAdapter.Of<bool>(Vehicle, nameof(AvsVehicle.isScuttled));

            _playerExit = new MethodAdapter<bool>(rmc, Vehicle, nameof(AvsVehicle.PlayerExit));
            _playerEntry = new MethodAdapter(rmc, Vehicle, nameof(AvsVehicle.PlayerEntry));
            _beginPiloting = new MethodAdapter(rmc, Vehicle, nameof(AvsVehicle.BeginMainHelmControl));
            _onVehicleUndocked = new MethodAdapter<bool, bool>(rmc, Vehicle, nameof(AvsVehicle.UndockVehicle));
            _onVehicleDocked = new MethodAdapter<Vector3, bool>(rmc, Vehicle, nameof(AvsVehicle.DockVehicle));
            _isPlayerControlling = new MethodAdapter(rmc, Vehicle, nameof(AvsVehicle.IsPlayerControlling));
            _onPilotModeEnd = new MethodAdapter(Rmc, Vehicle, "OnPilotModeEnd");

        }
        private readonly MethodAdapter<bool> _playerExit;
        private readonly MethodAdapter _playerEntry;
        private readonly MethodAdapter _beginPiloting;
        private readonly MethodAdapter<bool, bool> _onVehicleUndocked;
        private readonly MethodAdapter<Vector3, bool> _onVehicleDocked;
        private readonly PropertyAdapter<PingInstance> pingInstance;
        private readonly FieldAdapter<bool> _isScuttled;
        private readonly MethodAdapter _isPlayerControlling;
        private readonly MethodAdapter _onPilotModeEnd;

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
            if (pi.IsNull())
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
        public static bool Access(RootModController rmc, Vehicle vehicle, [NotNullWhen(true)] out AvsReflectionVehicle? outVehicle)
        {
            if (!vehicle.IsAvsVehicle())
            {
                outVehicle = null;
                return false;
            }
            outVehicle = new AvsReflectionVehicle(rmc, vehicle);
            return true;
        }

        public void OnVehicleUndocked(bool boardPlayer)
            => _onVehicleUndocked.Invoke(boardPlayer, true);
    }

}