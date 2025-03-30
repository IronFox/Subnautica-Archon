using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDockable : MonoBehaviour, IDockable
{
    public GameObject GameObject => base.gameObject;

    public void OnBeginDockAnimation()
    {}

    public void OnBeginUndockAnimation()
    {}

    public void OnEndDockAnimation()
    {}

    public void OnEndUndockAnimation()
    {}

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
