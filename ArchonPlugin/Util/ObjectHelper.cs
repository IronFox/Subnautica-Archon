using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Subnautica_Archon.Util
{
    public static class ObjectHelper
    {
        public static Transform? SafeGetTransform(this Vehicle? v)
        {
            if (v == null)
                return null;
            try
            {
                return v.transform;
            }
            catch (UnityException) //odd, but okay, don't care
            {
                return null;
            }
        }

        public static string GetPath(this Transform? t)
        {
            if (t == null)
                return "<null>";
            var parts = new List<string>();
            try
            {
                while (t != null)
                {
                    parts.Add($"{t.name}[{t.GetInstanceID()}]");
                    t = t.parent;
                }
            }
            catch (UnityException)  //odd, but okay, don't care
            { }
            parts.Reverse();
            return string.Join("/", parts);

        }
        public static string GetPath(this Component c)
        {
            try
            {
                return GetPath(c.transform) + $":{c.name}[{c.GetInstanceID()}]({c.GetType()})";
            }
            catch (Exception)
            {
                try
                {
                    return c.name;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }

        public static string GetVehicleName(this Vehicle v)
            => v.subName ? v.subName.GetName() : v.vehicleName;

        public static IReadOnlyList<Type> GetHierarchyOf(Type t)
        {
            if (t is null)
            {
                throw new ArgumentNullException(nameof(t));
            }
            var types = new List<Type>();
            while (t != null)
            {
                types.Add(t);
                t = t.BaseType;
            }
            return types;

        }

        public static bool IsDrone(this Vehicle vehicle)
        {
            if (vehicle == null)
            {
                return false;
            }
            return GetHierarchyOf(vehicle.GetType())
                .Any(type => type.Name == "Drone");
        }

        public static bool IsVFVehicle(this Vehicle vehicle)
        {
            if (vehicle == null)
            {
                return false;
            }
            return GetHierarchyOf(vehicle.GetType())
                .Any(type => type.Name == "ModVehicle");
        }

        public static bool IsAvsVehicle(this Vehicle vehicle)
        {
            if (vehicle == null)
            {
                return false;
            }
            return GetHierarchyOf(vehicle.GetType())
                .Any(type => type.Name == "AvsVehicle");
        }
    }
}
