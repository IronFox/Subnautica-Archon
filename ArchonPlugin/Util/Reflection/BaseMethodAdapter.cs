using AVS;
using AVS.Log;
using AVS.Util;
using System;
using System.Linq;
using System.Reflection;

namespace Subnautica_Archon.Util.Reflection
{
    public class BaseMethodAdapter
    {
        public bool IsEmpty => Method.IsNull() || !Target;
        protected MethodInfo? Method { get; }
        public RootModController Rmc { get; }
        protected UnityEngine.Object Target { get; }



        protected BaseMethodAdapter(RootModController rmc, UnityEngine.Object target, string methodName, bool ignoreMissing, params Type[] parameterTypes)
        {
            Rmc = rmc;
            Target = target;
            MethodName = methodName;
            using var log = SmartLog.For(rmc, parameters: Params.Of(target, methodName, ignoreMissing, parameterTypes));
            if (target.IsNull())
            {
                log.Error("Given target game object is empty");
                return;
            }
            if (string.IsNullOrEmpty(methodName))
            {
                log.Error("Given method name is empty");
                return;
            }
            try
            {
                Method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, binder: null, parameterTypes, modifiers: null);
                if (Method.IsNull())
                {
                    if (!ignoreMissing)
                        log.Error($"Unable to find method {methodName} with parameters ({string.Join(", ", parameterTypes.Select(x => x.ToString()))}) on object of type {target.GetType()}");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to get method {methodName} on {Target}: {ex}", ex);
            }
        }

        public string MethodName { get; set; }

        public override string ToString()
            => MethodName;

        public void Invoke(params object?[] p)
        {
            if (Method.IsNull())
                return;
            using var log = SmartLog.For(Rmc);
            if (!Target)
            {
                log.Error($"Unable to invoke method {Method.Name} since owning object has expired");
                return;
            }
            try
            {
                log.Debug($"Invoking method {Method.Name} on {Target} with parameters ({string.Join(", ", p)})");
                Method.Invoke(Target, p);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to invoke method {Method.Name} on {Target}: {ex}", ex);
            }
        }
    }

    public class MethodAdapter(RootModController rmc, UnityEngine.Object target, string methodName, bool ignoreMissing = false)
        : BaseMethodAdapter(rmc, target, methodName, ignoreMissing)
    {
        public void Invoke()
        {
            base.Invoke();
        }
    }
    public class MethodAdapter<T>(RootModController rmc, UnityEngine.Object target, string methodName, bool ignoreMissing = false)
        : BaseMethodAdapter(rmc, target, methodName, ignoreMissing, typeof(T))
    {
        public void Invoke(T p)
        {
            base.Invoke(p);
        }
    }
    public class MethodAdapter<T0, T1> : BaseMethodAdapter
    {
        public MethodAdapter(RootModController rmc, UnityEngine.Object target, string methodName, bool ignoreMissing = false)
            : base(rmc, target, methodName, ignoreMissing, typeof(T0), typeof(T1))
        { }

        public void Invoke(T0 p0, T1 p1)
        {
            base.Invoke(p0, p1);
        }
    }
    public class MethodAdapter<T0, T1, T2> : BaseMethodAdapter
    {
        public MethodAdapter(RootModController rmc, UnityEngine.Object target, string methodName, bool ignoreMissing = false)
            : base(rmc, target, methodName, ignoreMissing, typeof(T0), typeof(T1), typeof(T2))
        { }

        public void Invoke(T0 p0, T1 p1, T2 p2)
        {
            base.Invoke(p0, p1, p2);
        }
    }
    public class MethodAdapter<T0, T1, T2, T3> : BaseMethodAdapter
    {
        public MethodAdapter(RootModController rmc, UnityEngine.Object target, string methodName, bool ignoreMissing = false)
            : base(rmc, target, methodName, ignoreMissing, typeof(T0), typeof(T1), typeof(T2), typeof(T3))
        { }

        public void Invoke(T0 p0, T1 p1, T2 p2, T3 p3)
        {
            base.Invoke(p0, p1, p2, p3);
        }
    }
}
