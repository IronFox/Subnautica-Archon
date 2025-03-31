using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDockable : MonoBehaviour, IDockable
{
    public GameObject GameObject => base.gameObject;

    public bool ShouldUnfreezeImmediately => false;

    public void BeginDocking()
    {}

    public void EndDocking()
    {}

    public void BeginUndocking()
    {}

    public void EndUndocking()
    {}

    public IEnumerable<T> GetAllComponents<T>() where T : Component
        => gameObject.GetComponentsInChildren<T>();


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
}
