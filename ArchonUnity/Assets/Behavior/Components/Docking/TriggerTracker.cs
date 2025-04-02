#define DebugTracked
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public class TriggerTracker : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private ComponentSet<Collider> Set { get; } = new ComponentSet<Collider>(c => c.enabled && !c.isTrigger);

    private LogConfig logConfig = LogConfig.Default;
#if DebugTracked
    private int trackedVersion;
    public Collider[] tracked;
#endif
    public IEnumerable<Collider> CurrentlyTouching => Set;

    //public Collider GetFirstOrDefault(Func<Collider, bool> predicate)
    //{
    //    foreach (var collider in CurrentlyTouching)
    //        if (!predicate(collider))
    //            return collider;
    //    return null;
    //}

    void Awake()
    {
        Set.StartCleaningFrom(this);
    }
    // Update is called once per frame
    void Update()
    {
        Set.UpdateIfChanged(ref trackedVersion, ref tracked);
    }

    void OnDestroy()
    {
        Set.Dispose();
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (!other.transform.IsChildOf(exclude))
        {
            Set.Add(other);
            logConfig.Write("Registered entering "+other);
#if DebugTracked
            tracked = Set.ToArray();
#endif
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Set.Remove(other);
        logConfig.Write("Registered leaving " + other);
#if DebugTracked
        tracked = Set.ToArray();
#endif
    }

    internal T ClosestEnabledNonKinematic<T>(Func<Collider, T> converter)
        where T:class
    {
        float dist = float.MaxValue;
        T rs = null;
        foreach (var c in CurrentlyTouching)
        {
            if (!c.enabled || !c.attachedRigidbody || c.attachedRigidbody.isKinematic)
                continue;
            var closest = c.ClosestPoint(transform.position);
            var dist2 = M.SqrDistance(transform.position, closest);
            if (dist2 < dist)
            {
                var candidate = converter(c);
                if (candidate != null)
                {
                    dist = dist2;
                    rs = candidate;
                }
            }
        }
        return rs;
    }

    internal bool IsTracked(GameObject gameObject)
    {
        foreach (var c in CurrentlyTouching)
            if (c.gameObject == gameObject || (c.attachedRigidbody && c.attachedRigidbody.gameObject == gameObject))
                return true;
        return false;
    }
}
