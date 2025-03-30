using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tug : MonoBehaviour
{
    public BayControl Owner { get; private set; }
    public TugStatus Status { get; private set; }
    public IDockable Dockable { get; private set; }

    private LogConfig Log { get; set; } = LogConfig.Default;

    private List<Collider> DisabledColliders { get; } = new List<Collider>();
    private List<Renderer> DisabledRenderers { get; } = new List<Renderer>();
    private List<Rigidbody> KinematicRBs { get; } = new List<Rigidbody>();
    private List<Light> DisabledLights { get; } = new List<Light>();
    private List<MonoBehaviour> Disabled { get; } = new List<MonoBehaviour>();
    private List<ParticleSystem> DisabledEmitters { get; } = new List<ParticleSystem>();
    public Vector3 StartPosition { get; private set; }
    public Quaternion StartRotation { get; private set; }
    public Vector3 EndPosition { get; private set; }
    public Quaternion EndRotation { get; private set; }
    public float AnimationProgress { get; private set; }

    internal void Bind(BayControl bayControl, IDockable dockable, TugStatus status)
    {
        Log = new LogConfig($"Tug[{GetInstanceID()}]",true);
        DisabledColliders.Clear();
        KinematicRBs.Clear();
        Disabled.Clear();

        Owner = bayControl;
        Status = status;
        Dockable = dockable;

        if (!dockable.GameObject)
        {
            Dockable = null;
            return;
        }
        switch (status)
        {
            case TugStatus.Loading:
                dockable.OnBeginDockAnimation();
                break;
            case TugStatus.Loaded:
                dockable.OnBeginDockAnimation();
                dockable.OnEndDockAnimation();
                break;
            case TugStatus.Unloading:
                dockable.OnBeginUndockAnimation();
                break;
        }
        foreach (var c in dockable.GameObject.GetComponentsInChildren<Collider>())
            if (c.enabled)
            {
                c.enabled = false;
                DisabledColliders.Add(c);
            }
        foreach (var c in dockable.GameObject.GetComponentsInChildren<Rigidbody>())
            if (!c.isKinematic)
            {
                c.SetKinematic();
                KinematicRBs.Add(c);
            }

        foreach (var c in dockable.GameObject.GetComponentsInChildren<MonoBehaviour>())
            if (c != this && c.enabled)
            {
                c.enabled = false;
                Disabled.Add(c);
            }

        var pos = dockable.GameObject.transform.position;
        var rot = dockable.GameObject.transform.rotation;
        dockable.GameObject.transform.parent = bayControl.loaded;
        dockable.GameObject.transform.position = pos;
        dockable.GameObject.transform.rotation = rot;

        StartPosition = dockable.GameObject.transform.localPosition;
        StartRotation = dockable.GameObject.transform.localRotation;


        AnimationProgress = 0f;

        if (status == TugStatus.Loaded)
            TransitionToLoaded();

        if (status == TugStatus.Unloading)
        {
            EndPosition = Owner.dockingTrigger.transform.localPosition;
            EndRotation = Owner.dockingTrigger.transform.localRotation;
        }
        else
        {
            EndPosition = Owner.dockedBounds.transform.localPosition;
            EndRotation = Owner.dockedBounds.transform.localRotation;
        }

    }

    public void Unload()
    {
        TransitionToUnloading();
        Dockable.OnBeginUndockAnimation();
    }

    private void TransitionToFree()
    {
        if (Status != TugStatus.Unloading)
            throw new InvalidOperationException($"Cannot transition to free from {Status}");
        Log.Write($"Free");
        Status = TugStatus.Free;

        var pos = Dockable.GameObject.transform.position;
        var rot = Dockable.GameObject.transform.rotation;
        Dockable.GameObject.transform.parent = Owner.subRoot.parent;
        Dockable.GameObject.transform.position = pos;
        Dockable.GameObject.transform.rotation = rot;


        foreach (var c in DisabledColliders)
            c.enabled = true;
        foreach (var c in KinematicRBs)
            c.UnsetKinematic();

        foreach (var c in Disabled)
            c.enabled = true;

        DisabledColliders.Clear();
        KinematicRBs.Clear();
        Disabled.Clear();
    }

    private void TransitionToLoaded()
    {
        Log.Write($"Loaded");
        Status = TugStatus.Loaded;
        DisabledRenderers.Clear();
        DisabledLights.Clear();
        DisabledEmitters.Clear();
        if (Dockable != null)
        {
            foreach (var c in Dockable.GameObject.GetComponentsInChildren<Renderer>())
                if (c.enabled)
                {
                    c.enabled = false;
                    DisabledRenderers.Add(c);
                }
            foreach (var c in Dockable.GameObject.GetComponentsInChildren<Light>())
                if (c.enabled)
                {
                    c.enabled = false;
                    DisabledLights.Add(c);
                }
            foreach (var c in Dockable.GameObject.GetComponentsInChildren<ParticleSystem>())
                if (c.emission.enabled)
                {
                    var em = c.emission;
                    em.enabled = false;
                    DisabledEmitters.Add(c);
                }
        }
    }

    private void TransitionToUnloading()
    {
        if (Status != TugStatus.Loaded)
            throw new InvalidOperationException($"Cannot transition to unloading from {Status}");
        Log.Write($"Unloading");

        Status = TugStatus.Unloading;
        foreach (var c in DisabledRenderers)
            c.enabled = true;
        foreach (var c in DisabledLights)
            c.enabled = true;
        foreach (var c in DisabledEmitters)
        {
            var em = c.emission;
            em.enabled = true;
        }

        DisabledRenderers.Clear();
        DisabledLights.Clear();
        DisabledEmitters.Clear();

        EndPosition = Owner.dockingTrigger.transform.localPosition;
        EndRotation = Owner.dockingTrigger.transform.localRotation;
        StartPosition = Owner.dockedBounds.transform.localPosition;
        StartRotation = Owner.dockedBounds.transform.localRotation;
        AnimationProgress = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (Status)
        {
            case TugStatus.Free:
                if (!Owner.dockingTrigger.IsTracked(Dockable.GameObject))
                {
                    Log.Write("No longer in trigger zone. Releasing");
                    Destroy(this);
                }
                break;
            case TugStatus.Loading:
            case TugStatus.Unloading:
                AnimationProgress += Time.deltaTime / Owner.dockingSeconds;
                if (AnimationProgress < 1)
                {
                    Dockable.GameObject.transform.localPosition = Vector3.Lerp(StartPosition, EndPosition, AnimationProgress);
                    Dockable.GameObject.transform.localRotation = Quaternion.Slerp(StartRotation, EndRotation, AnimationProgress);
                }
                else
                {
                    Log.Write($"Animation end reached");
                    Dockable.GameObject.transform.localPosition = EndPosition;
                    Dockable.GameObject.transform.localRotation = EndRotation;
                    if (Status == TugStatus.Loading)
                    {
                        Dockable.OnEndDockAnimation();
                        Status = TugStatus.PendingLoaded;
                        Owner.SignalDockingDone(this, TransitionToLoaded);
                    }
                    else
                    {
                        Dockable.OnEndUndockAnimation();
                        TransitionToFree();
                    }
                }


                break;
        }
    }
}

public enum TugStatus
{
    Loading,
    PendingLoaded,
    Loaded,
    Unloading,
    Free
}
