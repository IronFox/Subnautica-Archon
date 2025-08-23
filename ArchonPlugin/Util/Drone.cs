using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;
using AVS.Util;

namespace Subnautica_Archon.Util
{

    internal struct Void { };
    internal class SimpleMethodHelper<ReturnType>
    {
        public SimpleMethodHelper(string methodName, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            MethodName = methodName;
            BindingFlags = bindingFlags;
        }

        private MethodInfo? _methodInfo;
        public string MethodName { get; }
        public BindingFlags BindingFlags { get; }

        public ReturnType ExecuteOn(object? target, params object[] parameters)
        {
            if (target == null)
            {
                Log.Error("Target object == null");
                return default!;
            }
            if (_methodInfo.IsNull())
            {
                _methodInfo = target.GetType().GetMethod(MethodName, BindingFlags);
                if (_methodInfo.IsNull())
                {
                    Log.Error($"Unable to find method {MethodName} on {target.GetType()}");
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
                Log.Error($"Failed to invoke method {MethodName} on {target}: {ex}");
                Debug.LogException(ex);
                return default!;
            }
        }
    }



    public class Drone
    {
        private readonly SimpleMethodHelper<bool> _isPlayerControlling
            = new SimpleMethodHelper<bool>("IsPlayerControlling");
        private readonly SimpleMethodHelper<Void> _stopControlling
            = new SimpleMethodHelper<Void>("StopControlling");
        private FieldInfo? _isAsleepField;
        private PropertyInfo? _isAsleepProperty;
        private Drone(Vehicle vehicle)
        {
            Vehicle = vehicle;
        }

        public static bool IsOne(Vehicle vehicle)
            => vehicle.IsDrone();
        public static bool Access(Vehicle vehicle, [NotNullWhen(true)] out Drone? drone)
        {
            if (!vehicle.IsDrone())
            {
                drone = null;
                return false;
            }
            drone = new Drone(vehicle);
            return true;
        }

        private bool AllocateSleepingFieldOrProperty()
        {
            if (_isAsleepField.IsNull() && _isAsleepProperty.IsNull())
            {
                _isAsleepField = Vehicle.GetType().GetField("isAsleep", BindingFlags.Public | BindingFlags.Instance);
                if (_isAsleepField.IsNull())
                {
                    _isAsleepProperty = Vehicle.GetType().GetProperty("isAsleep", BindingFlags.Public | BindingFlags.Instance);
                    if (_isAsleepProperty.IsNull())
                    {
                        Log.Error($"Unable to find field or property isAsleep on {Vehicle.GetType()}");
                        return false;
                    }
                }
            }
            return true;
        }

        public bool isAsleep
        {
            get
            {
                if (!AllocateSleepingFieldOrProperty())
                    return false;
                return _isAsleepField != null
                    ? (bool)_isAsleepField.GetValue(Vehicle)
                    : (bool)_isAsleepProperty!.GetValue(Vehicle);
            }
            set
            {
                if (!AllocateSleepingFieldOrProperty())
                    return;
                if (_isAsleepField != null)
                {
                    _isAsleepField.SetValue(Vehicle, value);
                }
                else
                {
                    _isAsleepProperty!.SetValue(Vehicle, value);
                }
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


        public Vehicle Vehicle { get; }
    }
}
