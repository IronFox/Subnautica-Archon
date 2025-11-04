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
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (f.IsNull())
                log.Error($"Unable to find non-public field '{name}' on <{target.GetType()}> '{target}'");
            return new FieldAdapter<T>(rmc, f, target);
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
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (f.IsNull())
                log.Error($"Unable to find public field '{name}' on <{target.GetType()}> '{target}'");
            else if (f.FieldType != typeof(T))
            {
                log.Error($"Field '{name}' on <{target.GetType()}> '{target}' is of type {f.FieldType}, expected {typeof(T)}");
                return default;
            }
            return new FieldAdapter<T>(rmc, f, target);
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
            var f = FindField(target.GetType(), name, instance: true, ignoreCase: false, isPublic: true, isPrivate: true);
            if (f.IsNull())
                log.Error($"Unable to find field '{name}' on <{target.GetType()}> '{target}'");
            return new FieldAdapter<T>(rmc, f, target);
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
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var type = target.GetType();
            foreach (var name in names)
            {
                var f = FindField(type, name, instance: true, ignoreCase: false, isPublic: true, isPrivate: true);
                if (f.IsNotNull())
                {
                    log.Debug($"Found field '{name}' on <{target.GetType()}> '{target}'");
                    return new FieldAdapter<T>(rmc, f, target);
                }
            }
            log.Error($"Unable to find fields '{string.Join("','", names)}' on <{type}> '{target}' using flags: {flags}");
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                log.Debug($"Available field: '{f.Name}' of type '{f.FieldType}'");
            }

            while (type.BaseType.IsNotNull())
            {
                type = type.BaseType;
                log.Debug($"In {type.Name}:");
                foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    log.Debug($"Available field: '{f.Name}' of type '{f.FieldType}'");
                }
            }




            return new FieldAdapter<T>(rmc, null, target);
        }


        public static FieldInfo? FindField(Type type, string name, bool instance, bool ignoreCase, bool isPublic, bool isPrivate)
        {
            var flags = BindingFlags.Default;
            if (instance)
                flags |= BindingFlags.Instance;
            else
                flags |= BindingFlags.Static;
            if (ignoreCase)
                flags |= BindingFlags.IgnoreCase;
            if (isPublic)
                flags |= BindingFlags.Public;
            if (isPrivate)
                flags |= BindingFlags.NonPublic;

            while (type.IsNotNull())
            {
                var field = type.GetField(name, flags);
                if (field.IsNotNull())
                    return field;
                type = type.BaseType;
            }
            return null;
        }
    }

    public readonly record struct FieldAdapter<T>
    {
        public RootModController Rmc { get; }
        public FieldInfo? Field { get; }
        public object Target { get; }

        public bool IsValid => Field != null && Target != null;

        public FieldAdapter(RootModController rmc, FieldInfo? field, object target)
        {
            if (field != null && field.FieldType != typeof(T))
                throw new ArgumentException($"FieldAdapter is declared for type {typeof(T)} but field {field.Name} is of type {field.FieldType}");
            Rmc = rmc;
            Field = field;
            Target = target;
        }

        public void Set(T value)
        {
            if (Field.IsNotNull())
            {
                using var log = SmartLog.LazyFor(Rmc);

                try
                {
                    Field.SetValue(Target, value);
                    log.Debug($"Set field '{Field.Name}' on <{Target.GetType()}> '{Target}' to value '{value}'");
                }
                catch (Exception e)
                {
                    log.Error($"Unable to set field '{Field.Name}' on <{Target.GetType()}> '{Target}' to value '{value}': {e}");
                }
            }
        }

        public T Value => (T)(Field?.GetValue(Target) ?? default(T)!);

        public static implicit operator T(FieldAdapter<T> a) => a.Value;
    }
}
