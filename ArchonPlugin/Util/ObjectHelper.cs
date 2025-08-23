using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AVS.Util;

namespace Subnautica_Archon.Util
{
    public static class ObjectHelper
    {
        public static Transform? SafeGetTransform(this Vehicle? v)
        {
            if (v.IsNull())
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
            if (t.IsNull())
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
        
        public static IReadOnlyList<Type> GetHierarchyOf(Type? t)
        {
            if (t.IsNull())
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
            if (vehicle.IsNull())
            {
                return false;
            }
            return GetHierarchyOf(vehicle.GetType())
                .Any(type => type.Name == "Drone");
        }
        public static Type? DroneType(this Vehicle vehicle)
        {
            if (vehicle.IsNull())
            {
                return null;
            }
            return GetHierarchyOf(vehicle.GetType())
                .FirstOrDefault(type => type.Name == "Drone")
                ;
        }

        public static bool IsVFVehicle(this Vehicle vehicle)
        {
            if (vehicle.IsNull())
            {
                return false;
            }
            return GetHierarchyOf(vehicle.GetType())
                .Any(type => type.Name == "ModVehicle");
        }

        public static bool IsAvsVehicle(this Vehicle vehicle)
        {
            if (vehicle.IsNull())
            {
                return false;
            }
            return GetHierarchyOf(vehicle.GetType())
                .Any(type => type.Name == "AvsVehicle");
        }
    }
}
