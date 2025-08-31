using AVS;
using AVS.Log;
using AVS.Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

namespace Subnautica_Archon.Util
{

    internal struct Void { };
    internal class SimpleMethodHelper<ReturnType>
    {
        public SimpleMethodHelper(RootModController rmc, string methodName, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            RMC = rmc;
            MethodName = methodName;
            BindingFlags = bindingFlags;
        }

        private MethodInfo? _methodInfo;

        public RootModController RMC { get; }
        public string MethodName { get; }
        public BindingFlags BindingFlags { get; }

        public ReturnType ExecuteOn(object? target, params object[] parameters)
        {
            using var log = SmartLog.For(RMC);

            if (target == null)
            {
                log.Error("Target object == null");
                return default!;
            }
            if (_methodInfo.IsNull())
            {
                _methodInfo = target.GetType().GetMethod(MethodName, BindingFlags);
                if (_methodInfo.IsNull())
                {
                    log.Error($"Unable to find method {MethodName} on {target.GetType()}");
                    return default!;
                }
            }
            try
            {
                if (typeof(ReturnType) == typeof(Void))
                {
                    _methodInfo.Invoke(target, parameters);
                    return default!;
                }
                return (ReturnType)_methodInfo.Invoke(target, parameters);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to invoke method {MethodName} on {target}: {ex}");
                Debug.LogException(ex);
                return default!;
            }
        }
    }



    public class Drone
    {
        private readonly SimpleMethodHelper<bool> _isPlayerControlling;
        private readonly SimpleMethodHelper<Void> _stopControlling;
        private TernaryValue<FieldOrPropertyAdapter<bool>> _isAsleep;
        private Drone(RootModController rmc, Vehicle vehicle)
        {
            RMC = rmc;
            Vehicle = vehicle;
            _isPlayerControlling = new SimpleMethodHelper<bool>(rmc, "IsPlayerControlling");
            _stopControlling = new SimpleMethodHelper<Void>(rmc, "StopControlling");
        }

        public static bool IsOne(Vehicle vehicle)
            => vehicle.IsDrone();
        public static bool Access(RootModController rmc, Vehicle vehicle, [NotNullWhen(true)] out Drone? drone)
        {
            if (!vehicle.IsDrone())
            {
                drone = null;
                return false;
            }
            drone = new Drone(rmc, vehicle);
            return true;
        }

        private bool AllocateSleepingFieldOrProperty([NotNullWhen(true)] out FieldOrPropertyAdapter<bool> access)
        {
            if (!_isAsleep.IsSet)
            {
                _isAsleep.Set(FieldOrPropertyAdapter.OfPublic<bool>(RMC, Vehicle, "isAsleep"));
                if (!_isAsleep.Item!.Value.IsValid)
                {
                    _isAsleep.HasFailed = true;
                    access = default;
                    return false;
                }
            }
            access = _isAsleep.Item!.Value;
            return _isAsleep.IsSetNotFailed;
        }

        public bool IsAsleep
        {
            get
            {
                if (!AllocateSleepingFieldOrProperty(out var acc))
                    return false;
                return acc.Value;
            }
            set
            {
                if (!AllocateSleepingFieldOrProperty(out var acc))
                    return;
                acc.Set(value);
            }
        }

        public bool IsPlayerControlling()
        {
            return _isPlayerControlling.ExecuteOn(Vehicle);
        }

        public void StopControlling()
        {
            _stopControlling.ExecuteOn(Vehicle);
        }

        public RootModController RMC { get; }

        public Vehicle Vehicle { get; }

        public bool ClearMountedDrone(SmartLog ctx)
        {
            var t = Vehicle.DroneType();
            if (t.IsNull())
            {
                ctx.Warn($"Expected drone type but got none");
                return false;
            }

            ctx.Write($"Drone type is {t.FullName}");

            var mountedDroneField = t.GetField("mountedDrone", BindingFlags.Public | BindingFlags.Static);
            if (mountedDroneField.IsNull())
            {
                ctx.Warn($"Unable to find field mountedDrone on {t}");
                return false;
            }

            var value = mountedDroneField.GetValue(null) as Vehicle;
            if (value.IsNotNull())
            {
                ctx.Warn($"Mounted drone field reported as {value.NiceName()}");
                mountedDroneField.SetValue(null, null);
                return true;
            }
            ctx.Write($"mountedDrone is null");

            return false;
        }
    }
}
