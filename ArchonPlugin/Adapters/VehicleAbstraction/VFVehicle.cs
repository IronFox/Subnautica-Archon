using AVS;
using AVS.Log;
using AVS.Util;
using Subnautica_Archon.Util;
using Subnautica_Archon.Util.Reflection;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Void = Subnautica_Archon.Util.Void;

namespace Subnautica_Archon.Adapters.VehicleAbstraction
{
    internal class VFVehicle(RootModController rmc, Vehicle vehicle)
    {
        public RootModController RMC { get; } = rmc;
        public Vehicle Vehicle { get; } = vehicle;


        private readonly SimpleMethodHelper<Void> _playerExit
            = new SimpleMethodHelper<Void>(rmc, "PlayerExit");

        private readonly SimpleMethodHelper<Void> _playerEntry
            = new SimpleMethodHelper<Void>(rmc, "PlayerEntry");

        private readonly SimpleMethodHelper<Void> _beginPiloting
            = new SimpleMethodHelper<Void>(rmc, "BeginPiloting");

        private readonly SimpleMethodHelper<Void> _onVehicleUndocked
            = new SimpleMethodHelper<Void>(rmc, "OnVehicleUndocked");

        private Ternary<MethodAdapter<Vehicle, Vector3>> _onVehicleDocked0;
        private Ternary<MethodAdapter<Vector3>> _onVehicleDocked1;
        private readonly FieldAdapter<PingInstance> pingInstance = FieldAdapter.Of<PingInstance>(rmc, vehicle, "pingInstance");
        private readonly FieldAdapter<bool> _isScuttled = FieldAdapter.Of<bool>(rmc, vehicle, "isScuttled");
        private PropertyAdapter<bool> _isUnderCommand;


        public bool IsUnderCommand
        {
            get
            {
                if (!_isUnderCommand.IsValid)
                    _isUnderCommand = PropertyAdapter.OfPublic<bool>(RMC, Vehicle, "IsUnderCommand");
                return _isUnderCommand.Value;
            }
            set
            {
                if (!_isUnderCommand.IsValid)
                    _isUnderCommand = PropertyAdapter.OfPublic<bool>(RMC, Vehicle, "IsUnderCommand");
                _isUnderCommand.Set(value);
            }
        }

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

        private bool TryCall<T>(ref Ternary<T> ternary, params object?[] args) where T : BaseMethodAdapter
        {
            using var log = SmartLog.LazyFor(RMC);
            if (ternary.IsSetNotFailed)
                try
                {
                    ternary.Item!.Invoke(args);
                    return true;
                }
                catch (MissingMethodException)
                {
                    log.Warn($"{ternary.Item} does not exist after all");
                    ternary.HasFailed = true;
                }

            return false;

        }

        public void OnVehicleDocked(Vector3 exitLocation)
        {
            using var log = SmartLog.LazyFor(RMC);
            if (!_onVehicleDocked0.IsSet)
                _onVehicleDocked0.Set(new MethodAdapter<Vehicle, Vector3>(RMC, Vehicle, "OnVehicleDocked", ignoreMissing: true));
            if (!_onVehicleDocked1.IsSet)
                _onVehicleDocked1.Set(new MethodAdapter<Vector3>(RMC, Vehicle, "OnVehicleDocked", ignoreMissing: true));
            if (!TryCall(ref _onVehicleDocked0, Vehicle, exitLocation)
                && !TryCall(ref _onVehicleDocked1, Vehicle))
                log.Error("OnVehicleDocked method not found on Vehicle");
        }

        public PingInstance HudPingInstance
        {
            get
            {
                var pi = pingInstance.Value;
                if (pi.IsNull())
                    return Vehicle.subName.pingInstance;
                return pi;
            }
        }

        public bool HudIconIsEnabled()
        {
            using var log = SmartLog.LazyFor(RMC);
            var pi = pingInstance.Value;
            if (pi.IsNull())
            {
                log.Error("pingInstance not set on " + Vehicle.NiceName());
                return false;
            }
            return pi.enabled || pi.visible;
        }

        public void SetHudIcon(bool visible)
        {
            using var log = SmartLog.LazyFor(RMC);
            var pi = pingInstance.Value;
            if (pi.IsNull())
            {
                log.Error("pingInstance not set on " + Vehicle.NiceName());
                return;
            }
            pi.SetVisible(visible);
            pi.enabled = visible;

        }

        public bool isScuttled
        {
            get => _isScuttled.Value;
            set => _isScuttled.Set(value);
        }

        public static bool IsOne(Vehicle vehicle)
            => vehicle.IsVFVehicle();
        public static bool Access(RootModController rmc, Vehicle vehicle, [NotNullWhen(true)] out VFVehicle? outVehicle)
        {
            if (!vehicle.IsVFVehicle())
            {
                outVehicle = null;
                return false;
            }
            outVehicle = new VFVehicle(rmc, vehicle);
            return true;
        }

        public void OnVehicleUndocked()
            => _onVehicleUndocked.ExecuteOn(Vehicle);
    }
}
