using System;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectUtil
{

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