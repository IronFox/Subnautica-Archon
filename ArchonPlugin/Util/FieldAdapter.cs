using System;
using System.Reflection;

namespace Subnautica_Archon.Util
{
    public static class FieldAdapter
    {
        public static FieldAdapter<T> OfNonPublic<T>(object target, string name)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null)
                Log.Error($"Unable to find field '{name}' on <{target.GetType()}> '{target}'");
            return new FieldAdapter<T>(f, target);
        }

        public static FieldAdapter<T> OfNonPublic<T>(UnityEngine.Object target, string name)
        {
            if (!target)
                return default;
            return OfNonPublic<T>((object)target, name);
        }
        public static FieldAdapter<T> OfPublic<T>(object target, string name)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            if (f == null)
                Log.Error($"Unable to find field '{name}' on <{target.GetType()}> '{target}'");
            return new FieldAdapter<T>(f, target);
        }

        public static FieldAdapter<T> OfPublic<T>(UnityEngine.Object target, string name)
        {
            if (!target)
                return default;
            return OfPublic<T>((object)target, name);
        }
    }

    public readonly struct FieldAdapter<T>
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
