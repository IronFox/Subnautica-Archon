using AVS;
using AVS.Log;
using AVS.Util;
using System;
using System.Reflection;

namespace Subnautica_Archon.Util
{
    public static class FieldOrPropertyAdapter
    {
        public static FieldOrPropertyAdapter<T> OfNonPublic<T>(RootModController rmc, object target, string name)
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (p.IsNull() && f.IsNull())
            {
                using var log = SmartLog.For(rmc);
                log.Error($"Unable to find non-public property/field '{name}' on <{target.GetType()}> '{target}'");
            }
            return new FieldOrPropertyAdapter<T>(p, f, target);
        }

        public static FieldOrPropertyAdapter<T> OfNonPublic<T>(RootModController rmc, UnityEngine.Object target, string name)
        {
            if (!target)
                return default;
            return OfNonPublic<T>(rmc, (object)target, name);
        }
        public static FieldOrPropertyAdapter<T> OfPublic<T>(RootModController rmc, object target, string name)
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            if (p.IsNull())
            {
                using var log = SmartLog.For(rmc);
                log.Error($"Unable to find public property/field '{name}' on <{target.GetType()}> '{target}'");
            }
            return new FieldOrPropertyAdapter<T>(p, f, target);
        }

        public static FieldOrPropertyAdapter<T> OfPublic<T>(RootModController rmc, UnityEngine.Object target, string name)
        {
            if (!target)
                return default;
            return OfPublic<T>(rmc, (object)target, name);
        }
    }

    public readonly struct FieldOrPropertyAdapter<T>
    {
        public PropertyInfo? Property { get; }
        public FieldInfo? Field { get; }
        public object Target { get; }

        public bool IsValid => (Property.IsNotNull() || Field.IsNotNull()) && Target != null;

        public FieldOrPropertyAdapter(PropertyInfo? property, FieldInfo? field, object target)
        {
            if (property.IsNotNull() && property.PropertyType != typeof(T))
                throw new ArgumentException($"FieldOrPropertyAdapter is declared for type {typeof(T)} but property {property.Name} is of type {property.PropertyType}");
            if (field.IsNotNull() && field.FieldType != typeof(T))
                throw new ArgumentException($"FieldOrPropertyAdapter is declared for type {typeof(T)} but field {field.Name} is of type {field.FieldType}");
            Property = property;
            Field = field;
            Target = target;
        }

        public void Set(T value)
        {
            if (Field.IsNotNull())
                Field.SetValue(Target, value);
            if (Property.IsNotNull())
                Property.SetValue(Target, value);
        }

        public T Value
        {
            get
            {
                if (Property.IsNotNull())
                    return Property.GetValue(Target) is T t ? t : default!;
                if (Field.IsNotNull())
                    return Field.GetValue(Target) is T t ? t : default!;
                return default!;
            }
        }

        public static implicit operator T(FieldOrPropertyAdapter<T> a) => a.Value;
    }
}
