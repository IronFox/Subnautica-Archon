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

    public Transform exclude;

    private LogConfig logConfig = LogConfig.Default;
    public Collider[] tracked;
    public IEnumerable<Collider> CurrentlyTouching => currentlyTouching.Values.Where(x => x);
    private readonly Dictionary<int, Collider> currentlyTouching = new Dictionary<int, Collider>();
    private int checkProgress = 0;
    // Update is called once per frame
    void Update()
    {
        if (--checkProgress < 0)
        {
            List<int> remove = null;
            foreach (var c in currentlyTouching)
                if (!c.Value || !c.Value.enabled)
                    (remove ?? (remove = new List<int>())).Add(c.Key);
            if (remove != null)
                foreach (var c in remove)
                    currentlyTouching.Remove(c);
            tracked = currentlyTouching.Values.ToArray();
            checkProgress = 10;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (!other.transform.IsChildOf(exclude))
        {
            currentlyTouching[other.GetInstanceID()] = other;
            logConfig.Write("Registered entering "+other);
            tracked = currentlyTouching.Values.ToArray();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        currentlyTouching.Remove(other.GetInstanceID());
        logConfig.Write("Registered leaving " + other);
        tracked = currentlyTouching.Values.ToArray();
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
