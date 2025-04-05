using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDockable : MonoBehaviour, IDockable
{
    public bool undockUpright = true;

    public GameObject GameObject => base.gameObject;

    public bool ShouldUnfreezeImmediately => false;

    public bool UndockUpright => undockUpright;

    public Bounds debugOutBounds;
    public Bounds debugOutBounds2;
    public Bounds LocalBounds { get; private set; }

    public HashSet<string> Tags { get; } = new HashSet<string>();

    public void BeginDocking()
    { }

    public void EndDocking()
    { }

    public void BeginUndocking()
    { }

    public void EndUndocking()
    { }

    public IEnumerable<T> GetAllComponents<T>() where T : Component
        => gameObject.GetComponentsInChildren<T>();


    void Awake()
    {
        LocalBounds = debugOutBounds = transform.ComputeScaledLocalBounds(includeRenderers: false, includeColliders:true);
        debugOutBounds2 = transform.ComputeScaledLocalBounds(includeRenderers: true, includeColliders: false);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDockingDone()
    {}

    public void UpdateWaitingForBayDoorClose()
    {}

    public void PrepareUndocking()
    {}

    public void UpdateWaitingForBayDoorOpen()
    {}

    public void OnUndockingDone()
    {}

    public void RestoreDockedStateFromSaveGame()
    {}

    public void Tag(string tag)
    {
        Tags.Add(tag);
    }

    public void Untag(string tag)
    {
        Tags.Remove(tag);
    }

    public bool IsTagged(string tag)
    {
        return Tags.Contains(tag);
    }

    public void OnUndockedForSaving()
    {}

    public void OnRedockedAfterSaving()
    {}

    public IEnumerable<GameObject> GetAllObjects() => gameObject.GetAllChildren();
}
