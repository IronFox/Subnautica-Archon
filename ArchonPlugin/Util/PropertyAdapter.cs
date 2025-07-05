using System;
using System.Reflection;

namespace Subnautica_Archon.Util
{
    public static class PropertyAdapter
    {
        public static PropertyAdapter<T> OfNonPublic<T>(object target, string name)
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (p is null)
                Log.Error($"Unable to find field '{name}' on <{target.GetType()}> '{target}'");
            return new PropertyAdapter<T>(p, target);
        }

        public static PropertyAdapter<T> OfNonPublic<T>(UnityEngine.Object target, string name)
        {
            if (!target)
                return default;
            return OfNonPublic<T>((object)target, name);
        }
        public static PropertyAdapter<T> OfPublic<T>(object target, string name)
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (p is null)
                Log.Error($"Unable to find field '{name}' on <{target.GetType()}> '{target}'");
            return new PropertyAdapter<T>(p, target);
        }

        public static PropertyAdapter<T> OfPublic<T>(UnityEngine.Object target, string name)
        {
            if (!target)
                return default;
            return OfPublic<T>((object)target, name);
        }
    }

    public readonly struct PropertyAdapter<T>
    {
        public PropertyInfo Property { get; }
        public object Target { get; }

        public bool IsValid => Property != null && Target != null;

        public PropertyAdapter(PropertyInfo property, object target)
        {
            if (property.PropertyType != typeof(T))
                throw new ArgumentException($"PropertyAdapter is declared for type {typeof(T)} but property {property.Name} is of type {property.PropertyType}");
            Property = property;
            Target = target;
        }

        public void Set(T value)
        {
            Property?.SetValue(Target, value);
        }

        public T Value => (T)(Property?.GetValue(Target) ?? default(T));

        public static implicit operator T(PropertyAdapter<T> a) => a.Value;
    }
}
