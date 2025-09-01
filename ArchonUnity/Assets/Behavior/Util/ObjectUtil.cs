using Assets.Behavior.Adapters;
using Assets.Behavior.Util.Enabled;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Behavior.Util
{

    public static class ObjectUtil
    {

        public static bool DisableAllEnabled(this IEnumerable<IEnabled> enabled, Undoable.UndoableActions undo, bool forced = false)
        {
            bool rs = false;
            foreach (var c in enabled)
                rs |= undo.Do(new Undoable.DisableAction(c), forced);
            return rs;
        }

        public static bool EnableAllDisabled(this IEnumerable<IEnabled> enabled, Undoable.UndoableActions undo, bool forced = false)
        {
            bool rs = false;
            foreach (var c in enabled)
                rs |= undo.Do(new Undoable.EnableAction(c), forced);
            return rs;
        }

        public static IEnumerable<IEnabled> ToEnabled(this IEnumerable<Behaviour> behaviours)
            => behaviours.Select(x => new BehaviourEnabled(x));

        public static IEnumerable<IEnabled> ToEnabled(this IEnumerable<Collider> behaviours)
            => behaviours.Select(x => new ColliderEnabled(x));

        public static IEnumerable<IEnabled> ToEnabled(this IEnumerable<Renderer> behaviours)
            => behaviours.Select(x => new RendererEnabled(x));

        public static IEnumerable<IEnabled> ToForceRenderingOff(this IEnumerable<Renderer> behaviours)
            => behaviours.Select(x => new ForceRenderingOff(x));

        public static IEnumerable<IEnabled> ToEnabled(this IEnumerable<ParticleSystem> behaviours)
            => behaviours.Select(x => new EmissionEnabled(x));


        public static bool DisableAllEnabledColliders(this GameObject go, Undoable.UndoableActions undo, bool forced = false)
            => go.GetComponentsInChildren<Collider>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);
        public static bool DisableAllEnabledColliders(this Transform t, Undoable.UndoableActions undo, bool forced = false)
            => t.GetComponentsInChildren<Collider>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);
        public static bool DisableAllEnabledColliders(this IDockable dockable, Undoable.UndoableActions undo, bool forced = false)
            => dockable.GetAllComponents<Collider>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);
        public static bool DisableAllEnabledRenderers(this GameObject go, Undoable.UndoableActions undo, bool forced = false)
            => go.GetComponentsInChildren<Renderer>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);

        public static bool DisableAllEnabledRenderers(this Transform t, Undoable.UndoableActions undo, bool forced = false)
            => t.GetComponentsInChildren<Renderer>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);

        public static bool DisableAllEnabledRenderers(this IDockable dockable, Undoable.UndoableActions undo, bool forced = false)
            => dockable.GetAllComponents<Renderer>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);

        public static bool ForceDisableAllRenderers(this IDockable dockable, Undoable.UndoableActions undo, bool forced = false)
            => dockable.GetAllComponents<Renderer>()
            .ToForceRenderingOff()
            .EnableAllDisabled(undo, forced);

        public static bool DisableAllEnabledCanvases(this IDockable dockable, Undoable.UndoableActions undo, bool forced = false)
            => dockable.GetAllComponents<Canvas>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);

        public static bool DisableAllEnabledLights(this GameObject go, Undoable.UndoableActions undo, bool forced = false)
            => go.GetComponentsInChildren<Light>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);

        public static bool DisableAllEnabledLights(this Transform t, Undoable.UndoableActions undo, bool forced = false)
            => t.GetComponentsInChildren<Light>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);

        public static bool DisableAllEnabledLights(this IDockable dockable, Undoable.UndoableActions undo, bool forced = false)
            => dockable.GetAllComponents<Light>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);


        public static bool DisableAllActiveParticleEmitters(this GameObject go, Undoable.UndoableActions undo, bool forced = false)
            => go.GetComponentsInChildren<ParticleSystem>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);

        public static bool DisableAllActiveParticleEmitters(this Transform t, Undoable.UndoableActions undo, bool forced = false)
            => t.GetComponentsInChildren<ParticleSystem>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);

        public static bool DisableAllActiveParticleEmitters(this IDockable dockable, Undoable.UndoableActions undo, bool forced = false)
            => dockable.GetAllComponents<ParticleSystem>()
            .ToEnabled()
            .DisableAllEnabled(undo, forced);


        public static bool Disable(this IEnumerable<Rigidbody> rbs, Undoable.UndoableActions undo, bool forced = false)
        {
            bool rs = false;
            foreach (var c in rbs)
            {
                var batch = undo.GetOrAddBatch(c);
                rs |= batch.Do(new Undoable.DisableAction(new NonKinematic(c)), forced);
                rs |= batch.Do(new Undoable.DisableAction(new CollisionsEnabled(c)), forced);
                rs |= batch.Do(new Undoable.ZeroVelocityAction(c), forced);
            }
            return rs;
        }
        public static bool DisableRigidbodies(this GameObject go, Undoable.UndoableActions undo, bool forced = false)
            => go.GetComponentsInChildren<Rigidbody>().Disable(undo, forced);
        public static bool DisableRigidbodies(this Transform t, Undoable.UndoableActions undo, bool forced = false)
            => t.GetComponentsInChildren<Rigidbody>().Disable(undo, forced);
        public static bool DisableRigidbodies(this IDockable dockable, Undoable.UndoableActions undo, bool forced = false)
            => dockable.GetAllComponents<Rigidbody>().Disable(undo, forced);

        public static string NiceName(this UnityEngine.Object o)
        {
            if (!o)
                return $"<null>";
            var s = o.name;
            int at = s.IndexOf('(');
            if (at >= 0)
                s = s.Substring(0, at);
            return $"<{o.GetType().Name}> '{s}' [{o.GetInstanceID()}]";
        }



        public static string ComponentToString(this Component c, [CanBeNull] Transform terminator = null)
        {
            return c.transform.parent.PathToString(terminator, false) + '/' + c.NiceName();
        }

        public static string PathToString(this Transform t, [CanBeNull] Transform terminator = null, bool includeInstanceNumber = true)
        {
            if (!t)
                return "<null>";
            var parts = new List<string>();
            try
            {
                while (t && t != terminator)
                {
                    var s = t.name;
                    int at = s.IndexOf('(');
                    if (at >= 0)
                        s = s.Substring(0, at);
                    if (includeInstanceNumber)
                        parts.Add($"{s}[{t.GetInstanceID()}]");
                    else
                        parts.Add(s);
                    t = t.parent;
                }
            }
            catch (UnityException)  //odd, but okay, don't care
            { }
            parts.Reverse();
            return string.Join("/", parts);

        }

        public static IEnumerable<Transform> GetChildren(this Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
                yield return transform.GetChild(i);
        }
        public static GameObject GetGameObject(this Collider collider)
        {
            if (collider.attachedRigidbody)
                return collider.attachedRigidbody.gameObject;
            return collider.gameObject;
        }

        public static IEnumerable<T> GetAll<T>(this Transform t, GameObject exclude = null) where T : Component
        {
            //using (var log = new LogContext(nameof(GetAll)+$"<{typeof(T).Name}>", t, exclude))
            {
                if (t.gameObject == exclude)
                {
                    //log.Write("Excluded by exclusion object");
                    yield break;
                }

                if (t.GetComponent<ExcludeFromHierarchyChanges>())
                {
                    //log.Write($"Excluded by {nameof(ExcludeFromHierarchyChanges)}");
                    yield break;
                }

                foreach (var c in t.GetComponents<T>())
                {
                    //log.Write($"Found {c.NiceName()}");
                    yield return c;
                }

                foreach (var child in t.GetChildren())
                {
                    foreach (var c in GetAll<T>(child, exclude))
                    {
                        yield return c;
                    }
                }
            }
        }

        public static IEnumerable<Collider> GetAllColliders(this Transform t, GameObject exclude)
        {
            return GetAll<Collider>(t, exclude);
        }

        public static void RequireActive(this MonoBehaviour c, Transform rootTransform)
        {
            if (c.isActiveAndEnabled)
                return;
            if (!c.enabled)
            {
                using (var log = Log.New())
                    log.Error($"{c.NiceName()} has been disabled. Re-enabling");
                c.enabled = true;
            }
            if (c.isActiveAndEnabled)
                return;
            RequireActive(c.gameObject, rootTransform, () => c.isActiveAndEnabled);


        }

        public static void RequireActive(this GameObject o, Transform rootTransform, Func<bool> testFunction = null)
        {
            if (o.activeInHierarchy)
                return;
            testFunction = testFunction ?? (() => o.activeInHierarchy);
            var current = o.transform;
            while (current && current != rootTransform)
            {
                if (!current.gameObject.activeSelf)
                {
                    using (var log = Log.New())
                        log.Error($"{current.gameObject.NiceName()} has been deactivated. Re-activating");
                    current.gameObject.SetActive(false);

                    if (testFunction())
                        return;
                }
                current = current.parent;
            }

            if (!rootTransform.gameObject.activeSelf)
            {
                using (var log = Log.New())
                    log.Error($"{rootTransform.gameObject.NiceName()} has been deactivated. Re-activating");
                rootTransform.gameObject.SetActive(true);
            }

        }
    }
}