using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using AVS.Util;

namespace Subnautica_Archon.Util
{
    public static class Helper
    {
        public static void ChangeAvatarInput(LogContext ctx, bool active)
        {
            
            ctx.Write($"Changing avatar input: {active}");
            AvatarInputHandler.main.gameObject.SetActive(active);
        }
        public static PlayerReference GetPlayerReference()
        {
            return new PlayerReference(Player.mainObject, Player.main.camRoot.transform, 2f);
        }

        public static IEnumerable<Transform> Children(Transform t)
        {
            if (t.IsNull())
                yield break;
            for (int i = 0; i < t.childCount; i++)
                yield return t.GetChild(i);
        }


        public static IEnumerable<Component> AllComponents(Transform t)
        {
            if (t.IsNull())
                return [];

            return t.GetComponents<Component>();
        }
        public static IEnumerable<string> Names(IEnumerable<UnityEngine.Object> source)
        {
            foreach (var obj in source)
                if (obj.IsNull())
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
                if (obj.IsNull())
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

        internal static void SetHudIcon(this PingInstance pingInstance, LogContext ctx, bool visible)
        {
            ctx.Write($"Setting ping icon {pingInstance.NiceName()} to {visible}");
            pingInstance.SetVisible(visible);
            pingInstance.enabled = visible;
            if (visible && !pingInstance.gameObject.activeInHierarchy)
                ctx.Warn($"Ping instance gameObject is not active. The icon will still be invisible");
        }

    }
}
