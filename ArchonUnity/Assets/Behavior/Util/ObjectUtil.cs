using System;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectUtil
{
    public static void DisableAllEnabled(this IEnumerable<Collider> colliders, Undoable undo)
    {
        foreach (var c in colliders)
            if (c.enabled)
            {
                c.enabled = false;
                undo.Add(() => c.enabled = true);
            }
    }
    public static void DisableAllEnabledColliders(this GameObject go, Undoable undo)
        => go.GetComponentsInChildren<Collider>().DisableAllEnabled(undo);
    public static void DisableAllEnabledColliders(this Transform t, Undoable undo)
        => t.GetComponentsInChildren<Collider>().DisableAllEnabled(undo);
    public static void DisableAllEnabledColliders(this IDockable dockable, Undoable undo)
        => dockable.GetAllComponents<Collider>().DisableAllEnabled(undo);

    public static void Disable(this IEnumerable<Rigidbody> rbs, Undoable undo)
    {
        foreach (var c in rbs)
        {
            if (!c.isKinematic)
            {
                LogConfig.Default.Write($"Disabling rigidbody [{c}]");
                c.SetKinematic();
                undo.Add(() => {
                    LogConfig.Default.Write($"Re-enabling rigidbody [{c}]");
                    c.UnsetKinematic();
                });
            }
            if (c.detectCollisions)
            {
                LogConfig.Default.Write($"Disabling collisions of [{c}]");
                c.detectCollisions = false;
                undo.Add(() => {
                    LogConfig.Default.Write($"Re-enabling collisions of [{c}]");
                    c.detectCollisions = true;
                });
            }
            if (c.velocity.sqrMagnitude > 0)
            {
                LogConfig.Default.Write($"Clearing velocity of [{c}]");

                c.velocity = Vector3.zero;
            }
        }
    }
    public static void DisableRigidbodies(this GameObject go, Undoable undo)
        => go.GetComponentsInChildren<Rigidbody>().Disable(undo);
    public static void DisableRigidbodies(this Transform t, Undoable undo)
        => t.GetComponentsInChildren<Rigidbody>().Disable(undo);
    public static void DisableRigidbodies(this IDockable dockable, Undoable undo)
        => dockable.GetAllComponents<Rigidbody>().Disable(undo);

    public static string NiceName(this UnityEngine.Object o)
    {
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