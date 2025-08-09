using Assets.Behavior.Adapters;
using System.Collections.Generic;
using UnityEngine;

public class ColliderWatchdog : MonoBehaviour
{
    private IEnumerator<Collider> enumerator;
    private bool currentlyInDisabled = true;
    // Start is called before the first frame update
    void Start()
    {

    }

    private void MonitorNext()
    {
        if (enumerator == null)
        {
            currentlyInDisabled = !currentlyInDisabled;
            enumerator = currentlyInDisabled
                ? DisabledColliders.GetEnumerator()
                : EnabledColliders.GetEnumerator();
        }
        if (!enumerator.MoveNext())
        {
            enumerator.Dispose();
            enumerator = null;
            return;
        }
        var c = enumerator.Current;

        if (currentlyInDisabled)
        {
            if (c.enabled)
            {
                Log.LogWarning($"{nameof(ColliderWatchdog)}: Disabling collider {c.NiceName()}");
                c.enabled = false;
            }
        }
        else
        {
            if (!c.enabled)
            {
                Log.LogWarning($"{nameof(ColliderWatchdog)}: Enabling collider {c.NiceName()}");
                c.enabled = true;
            }
            if (!c.gameObject.activeInHierarchy)
            {
                Log.LogWarning($"{nameof(ColliderWatchdog)}: Collider game object {c.gameObject.NiceName()} has been disabled. Fixing");
                c.gameObject.RequireActive(transform);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < 10; i++)
        {
            MonitorNext();
        }
    }
    internal void SetCollidersEnabled(IEnumerable<Collider> colliders, bool enable)
    {
        foreach (var c in colliders)
        {
            if (c)
            {
                if (enable)
                {
                    EnabledColliders.Add(c);
                    DisabledColliders.Remove(c);
                    enumerator = null;
                    c.enabled = true;
                }
                else
                {
                    DisabledColliders.Add(c);
                    EnabledColliders.Remove(c);
                    enumerator = null;
                    c.enabled = false;
                }
            }
        }
    }

    private ComponentSet<Collider> EnabledColliders { get; } = new ComponentSet<Collider>();
    private ComponentSet<Collider> DisabledColliders { get; } = new ComponentSet<Collider>();

}
