using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ObjectUtil
{

    public static bool DisableAllEnabled(this IEnumerable<IEnabled> enabled, Undoable undo)
    {
        bool rs= false;
        foreach (var c in enabled)
            rs |= undo.Do(new DisableAction(c));
        return rs;
    }

    public static IEnumerable<IEnabled> ToEnabled(this IEnumerable<Behaviour> behaviours)
        => behaviours.Select(x => new BehaviourEnabled(x));
    
    public static IEnumerable<IEnabled> ToEnabled(this IEnumerable<Collider> behaviours)
        => behaviours.Select(x => new ColliderEnabled(x));
        
    public static IEnumerable<IEnabled> ToEnabled(this IEnumerable<Renderer> behaviours)
        => behaviours.Select(x => new RendererEnabled(x));
            
    public static IEnumerable<IEnabled> ToEnabled(this IEnumerable<ParticleSystem> behaviours)
        => behaviours.Select(x => new EmissionEnabled(x));


    public static bool DisableAllEnabledColliders(this GameObject go, Undoable undo)
        => go.GetComponentsInChildren<Collider>()
        .ToEnabled()
        .DisableAllEnabled(undo);
    public static bool DisableAllEnabledColliders(this Transform t, Undoable undo)
        => t.GetComponentsInChildren<Collider>()
        .ToEnabled()
        .DisableAllEnabled(undo);
    public static bool DisableAllEnabledColliders(this IDockable dockable, Undoable undo)
        => dockable.GetAllComponents<Collider>()
        .ToEnabled()
        .DisableAllEnabled(undo);
    public static bool DisableAllEnabledRenderers(this GameObject go, Undoable undo)
        => go.GetComponentsInChildren<Renderer>()
        .ToEnabled()
        .DisableAllEnabled(undo);

    public static bool DisableAllEnabledRenderers(this Transform t, Undoable undo)
        => t.GetComponentsInChildren<Renderer>()
        .ToEnabled()
        .DisableAllEnabled(undo);

    public static bool DisableAllEnabledRenderers(this IDockable dockable, Undoable undo)
        => dockable.GetAllComponents<Renderer>()
        .ToEnabled()
        .DisableAllEnabled(undo);

    public static bool DisableAllEnabledLights(this GameObject go, Undoable undo)
        => go.GetComponentsInChildren<Light>()
        .ToEnabled()
        .DisableAllEnabled(undo);

    public static bool DisableAllEnabledLights(this Transform t, Undoable undo)
        => t.GetComponentsInChildren<Light>()
        .ToEnabled()
        .DisableAllEnabled(undo);

    public static bool DisableAllEnabledLights(this IDockable dockable, Undoable undo)
        => dockable.GetAllComponents<Light>()
        .ToEnabled()
        .DisableAllEnabled(undo);

    
    public static bool DisableAllActiveParticleEmitters(this GameObject go, Undoable undo)
        => go.GetComponentsInChildren<ParticleSystem>()
        .ToEnabled()
        .DisableAllEnabled(undo);

    public static bool DisableAllActiveParticleEmitters(this Transform t, Undoable undo)
        => t.GetComponentsInChildren<ParticleSystem>()
        .ToEnabled()
        .DisableAllEnabled(undo);

    public static bool DisableAllActiveParticleEmitters(this IDockable dockable, Undoable undo)
        => dockable.GetAllComponents<ParticleSystem>()
        .ToEnabled()
        .DisableAllEnabled(undo);


    public static bool Disable(this IEnumerable<Rigidbody> rbs, Undoable undo)
    {
        bool rs = false;
        foreach (var c in rbs)
        {
            var batch = undo.GetOrAddBatch(c);
            rs |= batch.Do(new DisableAction(new NonKinematic(c)));
            rs |= batch.Do(new DisableAction(new CollisionsEnabled(c)));
            rs |= batch.Do(new ZeroVelocityAction(c));
        }
        return rs;
    }
    public static bool DisableRigidbodies(this GameObject go, Undoable undo)
        => go.GetComponentsInChildren<Rigidbody>().Disable(undo);
    public static bool DisableRigidbodies(this Transform t, Undoable undo)
        => t.GetComponentsInChildren<Rigidbody>().Disable(undo);
    public static bool DisableRigidbodies(this IDockable dockable, Undoable undo)
        => dockable.GetAllComponents<Rigidbody>().Disable(undo);

    public static string NiceName(this UnityEngine.Object o)
    {
        if (!o)
            return $"<null>";
        var s = o.name;
        int at = s.IndexOf('(');
        if (at >= 0)
            return s.Substring(0, at);
        return s;
    }
    public static string PathToString(this Transform t)
    {
        if (!t)
            return "<null>";
        var parts = new List<string>();
        try
        {
            while (t)
            {
                parts.Add($"{t.name}[{t.GetInstanceID()}]");
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
    public static GameObject GetGameObjectOf(Collider collider)
    {
        if (collider.attachedRigidbody)
            return collider.attachedRigidbody.gameObject;
        return collider.gameObject;
    }

    public static void RequireActive(this MonoBehaviour c, Transform rootTransform)
    {
        if (c.isActiveAndEnabled)
            return;
        if (!c.enabled)
        {
            LogConfig.Default.LogError($"{c} has been disabled. Re-enabling");
            c.enabled = true;
        }
        if (c.isActiveAndEnabled)
            return;
        var current = c.transform;
        while (current && current != rootTransform)
        {
            if (!current.gameObject.activeSelf)
            {
                LogConfig.Default.LogError($"{current.gameObject} has been deactivate. Re-activating");
                current.gameObject.SetActive(false);

                if (c.isActiveAndEnabled)
                    return;
            }
            current = current.parent;
        }

        if (!rootTransform.gameObject.activeSelf)
        {
            LogConfig.Default.LogError($"{rootTransform.gameObject} has been deactivate. Re-activating");
            rootTransform.gameObject.SetActive(false);
        }

    }
}