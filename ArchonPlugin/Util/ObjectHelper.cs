using System;
using System.Collections.Generic;
using System.Linq;

namespace Subnautica_Archon.Util
{
    public static class ObjectHelper
    {
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
            if (vehicle is null)
            {
                return false;
            }
            return GetHierarchyOf(vehicle.GetType())
                .Any(type => type.Name == "Drone");
        }
    }
}
