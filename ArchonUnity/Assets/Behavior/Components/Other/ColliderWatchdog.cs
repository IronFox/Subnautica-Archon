using Assets.Behavior.Adapters;
using System.Collections.Generic;
using System.Linq;
using Behavior.Util.Log;
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
                Log.LogWarning($"{nameof(ColliderWatchdog)}: Disabling collider {c.ComponentToString(transform)}");
                c.enabled = false;
            }
        }
        else
        {
            if (!c.enabled)
            {
                Log.LogWarning($"{nameof(ColliderWatchdog)}: Enabling collider {c.ComponentToString(transform)}");
                c.enabled = true;
            }
            if (!c.gameObject.activeInHierarchy)
            {
                Log.LogWarning($"{nameof(ColliderWatchdog)}: Collider game object {c.ComponentToString(transform)} has been disabled. Fixing");
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
        var list = colliders.ToList();
        using (var log = new LogContext(nameof(ColliderWatchdog)+'.'+nameof(SetCollidersEnabled), list.Count, enable))
        {
            foreach (var c in list)
            {
                if (c)
                {
                    if (enable)
                    {
                        //if (!EnabledColliders.Contains(c) && !DisabledColliders.Contains(c))
                          //  log.Write($"Registering new as enabled: {c.ComponentToString(transform)}");
                        EnabledColliders.Add(c);
                        DisabledColliders.Remove(c);
                        enumerator = null;
                        c.enabled = true;
                    }
                    else
                    {
                        //if (!EnabledColliders.Contains(c) && !DisabledColliders.Contains(c))
                          //  log.Write($"Registering new as disabled: {c.ComponentToString(transform)}");
                        DisabledColliders.Add(c);
                        EnabledColliders.Remove(c);
                        enumerator = null;
                        c.enabled = false;
                    }
                }
            }

            log.Write($"Changes applied. Enabled now {EnabledColliders.Count()}, disabled now {DisabledColliders.Count()}");
        }
    }

    private ComponentSet<Collider> EnabledColliders { get; } = new ComponentSet<Collider>();
    private ComponentSet<Collider> DisabledColliders { get; } = new ComponentSet<Collider>();

}
