using System;
using System.Reflection;
using UnityEngine;

namespace Subnautica_Archon
{
    public class BaseMethodAdapter
    {
        public bool IsEmpty => Method is null || !Target;
        protected MethodInfo Method {get; }
        protected UnityEngine.Object Target { get; }



        protected BaseMethodAdapter(UnityEngine.Object target, string methodName, params Type[] parameterTypes)
        {
            Target = target;
            if (target == null)
            {
                Log.Error("Given target game object is empty");
                return;
            }
            if (string.IsNullOrEmpty(methodName))
            {
                Log.Error("Given method name is empty");
                return;
            }
            try
            {
                Method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, parameterTypes, modifiers: null);
                if (Method is null)
                {
                    Log.Error($"Unable to find method {methodName} on object of type {target.GetType()}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get method {methodName} on {Target}: {ex}");
                Debug.LogException(ex);
            }
        }

        protected void Invoke(params object[] p)
        {
            if (Method is null)
                return;
            if (!Target)
            {
                Log.Error($"Unable to invoke method {Method.Name} since owning object has expired");
                return;
            }
            try
            {
                Method.Invoke(Target, p);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to invoke method {Method.Name} on {Target}: {ex}");
                Debug.LogException(ex);
            }
        }
    }

    public class MethodAdapter : BaseMethodAdapter
    {
        public MethodAdapter(UnityEngine.Object target, string methodName)
            : base(target, methodName, Array.Empty<Type>())
        {

        }

        public void Invoke()
        {
            base.Invoke();
        }
    }
    public class MethodAdapter<T> : BaseMethodAdapter
    {
        public MethodAdapter(UnityEngine.Object target, string methodName)
            : base(target, methodName, typeof(T))
        { }

        public void Invoke(T p)
        {
            base.Invoke(p);
        }
    }
    public class MethodAdapter<T0, T1> : BaseMethodAdapter
    {
        public MethodAdapter(UnityEngine.Object target, string methodName)
            :base(target,methodName, typeof(T0), typeof(T1))
        {}

        public void Invoke(T0 p0, T1 p1)
        {
            base.Invoke(p0, p1);
        }
    }
    public class MethodAdapter<T0, T1, T2> : BaseMethodAdapter
    {
        public MethodAdapter(UnityEngine.Object target, string methodName)
            :base(target,methodName, typeof(T0), typeof(T1), typeof(T2))
        {}

        public void Invoke(T0 p0, T1 p1, T2 p2)
        {
            base.Invoke(p0, p1, p2);
        }
    }
    public class MethodAdapter<T0, T1, T2, T3> : BaseMethodAdapter
    {
        public MethodAdapter(UnityEngine.Object target, string methodName)
            : base(target, methodName, typeof(T0), typeof(T1), typeof(T2), typeof(T3))
        { }

        public void Invoke(T0 p0, T1 p1, T2 p2, T3 p3)
        {
            base.Invoke(p0, p1, p2, p3);
        }
    }
}
