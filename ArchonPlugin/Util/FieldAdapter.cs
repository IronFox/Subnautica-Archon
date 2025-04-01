using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
    }

    public readonly struct FieldAdapter<T>
    {
        public FieldInfo Field { get; }
        public object Target { get; }

        public FieldAdapter(FieldInfo field, object target)
        {
            if (field.FieldType != typeof(T))
                throw new ArgumentException($"FieldAdapter is declared for type {typeof(T)} but field {field.Name} is of type {field.FieldType}");
            Field = field;
            Target = target;
        }

        public void Set(T value)
        {
            Field?.SetValue(Target, value);
        }

        public T Value => (T)(Field?.GetValue(Target) ?? default(T));

        public static implicit operator T(FieldAdapter<T>a) => a.Value;
    }
}
