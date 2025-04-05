using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subnautica_Archon.Util
{
    public static class Helper
    {
        public static void ChangeAvatarInput(bool active)
        {
            Log.Write($"Changing avatar input: {active}");
            AvatarInputHandler.main.gameObject.SetActive(active);
        }
        public static PlayerReference GetPlayerReference()
        {
            return new PlayerReference(Player.mainObject, Player.main.camRoot.transform);
        }

        public static IEnumerable<Transform> Children(Transform t)
        {
            if (t == null)
                yield break;
            for (int i = 0; i < t.childCount; i++)
                yield return t.GetChild(i);
        }


        public static IEnumerable<Component> AllComponents(Transform t)
        {
            if (t == null)
                return Array.Empty<Component>();

            return t.GetComponents<Component>();
        }
        public static IEnumerable<string> Names(IEnumerable<UnityEngine.Object> source)
        {
            foreach (var obj in source)
                if (obj == null)
                    yield return "<null>";
                else
                    yield return obj.name;
        }
        public static string NamesS(IEnumerable<Component> source)
            => S(Names(source));
        public static IEnumerable<string> Names(IEnumerable<Component> source)
        {
            foreach (var obj in source)
            {
                if (obj == null)
                    yield return "<null>";
                else
                    yield return obj.name;
            }
        }
        public static IEnumerable<string> Names(IEnumerable<FieldInfo> source)
        {
            foreach (var obj in source)
                yield return obj.Name;
        }

        public static string S(IEnumerable<string> source)
            => string.Join(", ", source);

        public static Component FindComponentInChildren(Transform t, string componentTypeName)
        {
            var c = t.GetComponent(componentTypeName);
            if (c != null)
                return c;
            for (int i = 0; i < t.childCount; i++)
            {
                c = FindComponentInChildren(t.GetChild(i), componentTypeName);
                if (c != null)
                    return c;
            }
            return null;
        }


        public static T Clone<T>(T obj) where T : new()
        {
            T copy = new T();
            foreach (var f in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                Log.Write($"Duplicating property {f} on {obj} to {copy}");
                f.SetValue(copy, f.GetValue(obj));
            }
            foreach (var p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                if (p.CanWrite)
                {
                    Log.Write($"Duplicating property {p} on {obj} to {copy}");
                    p.SetValue(copy, p.GetValue(obj));
                }
                else
                    Log.Write($"Cannot duplicate property {p} on {obj} to {copy} (readonly)");


            return copy;
        }

        internal static void SetHudIcon(this PingInstance pingInstance, bool visible)
        {
            pingInstance.SetVisible(visible);
            pingInstance.enabled = visible;
        }

        public static string GetName(this Vehicle vehicle)
             => vehicle.subName ? vehicle.subName.GetName() : vehicle.vehicleName;
        public static void SetName(this Vehicle vehicle, string name)
        {
            if (!vehicle)
                return;
            Logging.Default.LogMessage($"Changing name of {vehicle.NiceName()} '{vehicle.GetName()}' -> '{name}'");
            if (vehicle.subName)
                vehicle.subName.SetName(name);
            vehicle.vehicleName = name;
        }

    }
}
