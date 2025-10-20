using AVS;
using AVS.Log;
using AVS.Util;
using System;
using System.Reflection;

namespace Subnautica_Archon.Util
{
    public static class FieldAdapter
    {
        public static FieldAdapter<T> OfNonPublic<T>(RootModController rmc, object target, string name)
        {
            using var log = SmartLog.LazyFor(rmc);
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f.IsNull())
                log.Error($"Unable to find non-public field '{name}' on <{target.GetType()}> '{target}'");
            return new FieldAdapter<T>(f, target);
        }

        public static FieldAdapter<T> OfNonPublic<T>(RootModController rmc, UnityEngine.Object target, string name)
        {
            using var log = SmartLog.LazyFor(rmc);
            if (!target)
                return default;
            return OfNonPublic<T>(rmc, (object)target, name);
        }
        public static FieldAdapter<T> OfPublic<T>(RootModController rmc, object target, string name)
        {
            using var log = SmartLog.LazyFor(rmc);
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            if (f.IsNull())
                log.Error($"Unable to find public field '{name}' on <{target.GetType()}> '{target}'");
            else if (f.FieldType != typeof(T))
            {
                log.Error($"Field '{name}' on <{target.GetType()}> '{target}' is of type {f.FieldType}, expected {typeof(T)}");
                return default;
            }
            return new FieldAdapter<T>(f, target);
        }

        public static FieldAdapter<T> OfPublic<T>(RootModController rmc, UnityEngine.Object target, string name)
        {
            if (!target)
                return default;
            return OfPublic<T>(rmc, (object)target, name);
        }
        public static FieldAdapter<T> Of<T>(RootModController rmc, object target, string name)
        {
            using var log = SmartLog.LazyFor(rmc);
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f.IsNull())
                log.Error($"Unable to find field '{name}' on <{target.GetType()}> '{target}'");
            return new FieldAdapter<T>(f, target);
        }

        public static FieldAdapter<T> Of<T>(RootModController rmc, UnityEngine.Object target, string name)
        {
            if (!target)
                return default;
            return Of<T>(rmc, (object)target, name);
        }

        public static FieldAdapter<T> Of<T>(RootModController rmc, UnityEngine.Object target, string[] names)
        {
            if (!target)
                return default;
            using var log = SmartLog.LazyFor(rmc);
            foreach (var name in names)
            {
                var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f.IsNotNull())
                    return new FieldAdapter<T>(f, target);
            }
            log.Error($"Unable to find fields '{string.Join("','",names)}' on <{target.GetType()}> '{target}'");
            return new FieldAdapter<T>(null, target);
        }
    }

    public readonly record struct FieldAdapter<T>
    {
        public FieldInfo? Field { get; }
        public object Target { get; }

        public bool IsValid => Field != null && Target != null;

        public FieldAdapter(FieldInfo? field, object target)
        {
            if (field != null && field.FieldType != typeof(T))
                throw new ArgumentException($"FieldAdapter is declared for type {typeof(T)} but field {field.Name} is of type {field.FieldType}");
            Field = field;
            Target = target;
        }

        public void Set(T value)
        {
            Field?.SetValue(Target, value);
        }

        public T Value => (T)(Field?.GetValue(Target) ?? default(T)!);

        public static implicit operator T(FieldAdapter<T> a) => a.Value;
    }
}
