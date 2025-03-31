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
    

    private List<Action> UndoTugging { get; } = new List<Action>();
    private List<Action> UndoDocked {get; } = new List<Action>();
    public TransformDescriptor AnimationStart { get; private set; }
    public TransformDescriptor AnimationEnd { get; private set; }
    public float AnimationSeconds { get; private set; }
    public float AnimationProgress { get; private set; }

    internal void Bind(BayControl bayControl, IDockable dockable, TugStatus status)
    {
        Log = new LogConfig($"Tug[{GetInstanceID()}]", true);
        UndoTugging.Clear();

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
            case TugStatus.Docking:
                dockable.BeginDocking();
                break;
            case TugStatus.Docked:
                dockable.BeginDocking();
                dockable.EndDocking();
                break;
            case TugStatus.Undocking:
                dockable.BeginUndocking();
                break;
        }
        foreach (var c in dockable.GetAllComponents<Collider>())
            if (c.enabled)
            {
                c.enabled = false;
                UndoTugging.Add(() => c.enabled = true);
            }
        foreach (var c in dockable.GetAllComponents<Rigidbody>())
        {
            if (!c.isKinematic)
            {
                c.SetKinematic();
                UndoTugging.Add(() => c.UnsetKinematic());
            }
            if (c.detectCollisions)
            {
                c.detectCollisions = false;
                UndoTugging.Add(() => c.detectCollisions = true);
            }
            c.velocity = Vector3.zero;
        }

        foreach (var c in dockable.GetAllComponents<MonoBehaviour>())
            if (c != this && c.enabled)
            {
                c.enabled = false;
                UndoTugging.Add(() => c.enabled = true);
            }

        var pos = dockable.GameObject.transform.position;
        var rot = dockable.GameObject.transform.rotation;
        dockable.GameObject.transform.parent = bayControl.loaded;
        dockable.GameObject.transform.position = pos;
        dockable.GameObject.transform.rotation = rot;


        if (status == TugStatus.Docked)
            TransitionToLoaded();

        if (status == TugStatus.Undocking)
        {
            BeginUndocking();
        }
        else
        {
            AnimationStart = TransformDescriptor.FromGlobal(dockable.GameObject.transform);
            AnimationEnd = TransformDescriptor.FromLocal(Owner.dockedBounds.transform);
        }
        RestartAnimation();

    }

    public void Undock()
    {
        BeginUndocking();
        Dockable.BeginUndocking();
    }

    private void TransitionToFree()
    {
        if (Status != TugStatus.Undocking)
            throw new InvalidOperationException($"Cannot transition to free from {Status}");
        Log.Write($"Free");
        Status = TugStatus.UndockedWaitingForTriggerExit;

        var pos = Dockable.GameObject.transform.position;
        var rot = Dockable.GameObject.transform.rotation;
        Dockable.GameObject.transform.parent = Owner.subRoot.transform.parent;
        Dockable.GameObject.transform.position = pos;
        Dockable.GameObject.transform.rotation = rot;


        foreach (var c in UndoTugging)
            c();
        UndoTugging.Clear();
    }

    private void TransitionToLoaded()
    {
        Log.Write($"Loaded");
        Status = TugStatus.Docked;
        UndoDocked.Clear();

        if (Dockable != null)
        {
            foreach (var c in Dockable.GetAllComponents<Renderer>())
                if (c.enabled)
                {
                    c.enabled = false;
                    UndoDocked.Add(() => c.enabled = true);
                }
            foreach (var c in Dockable.GetAllComponents<Light>())
                if (c.enabled)
                {
                    c.enabled = false;
                    UndoDocked.Add(() => c.enabled = true);
                }
            foreach (var c in Dockable.GetAllComponents<ParticleSystem>())
                if (c.emission.enabled)
                {
                    var em = c.emission;
                    em.enabled = false;
                    UndoDocked.Add(() =>
                    {
                        var em2 = c.emission;
                        em2.enabled = true;
                    });
                }
        }
    }

    private void BeginUndocking()
    {
        if (Status != TugStatus.Docked)
            throw new InvalidOperationException($"Cannot transition to unloading from {Status}");
        Log.Write($"Unloading");

        Status = TugStatus.Undocking;

        foreach (var c in UndoDocked)
            c();
        UndoDocked.Clear();

        Dockable.GameObject.transform.localPosition = Owner.dockedBounds.transform.localPosition;
        AnimationStart = TransformDescriptor.FromLocal(Dockable.GameObject.transform);
        AnimationEnd = TransformDescriptor.FromLocal(Owner.dockingTrigger.transform);
        RestartAnimation();
    }

    private Vector3 LocalPosition(TransformDescriptor desc)
    {
        switch (desc.Locality)
        {
            case TransformLocality.Local:
                return desc.Position;
            case TransformLocality.Global:
                return Owner.transform.InverseTransformPoint(desc.Position);
            default:
                return desc.Position;
        }
    }

    private TransformDescriptor Local(TransformDescriptor desc)
    {
        switch (desc.Locality)
        {
            case TransformLocality.Local:
                return desc;
            case TransformLocality.Global:
                return desc.Localize(Owner.transform);
            default:
                return desc;
        }
    }

    private void RestartAnimation()
    {
        AnimationProgress = 0;
        AnimationSeconds = M.Distance(LocalPosition(AnimationStart), LocalPosition(AnimationEnd)) / Owner.dockingMetersPerSecond;
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
            case TugStatus.UndockedWaitingForTriggerExit:
                if (!Owner.dockingTrigger.IsTracked(Dockable.GameObject))
                {
                    Log.Write("No longer in trigger zone. Releasing");
                    Destroy(this);
                }
                break;
            case TugStatus.Docking:
            case TugStatus.Undocking:
                AnimationProgress += Time.deltaTime / AnimationSeconds;
                if (AnimationProgress < 1)
                {
                    var start = Local(AnimationStart);
                    var end = Local(AnimationEnd);
                    TransformDescriptor
                        .Lerp(start, end, M.Smoothstep(0, 1, AnimationProgress))
                        .ApplyTo(Dockable.GameObject.transform);
                }
                else
                {
                    Log.Write($"Animation end reached");
                    Local(AnimationEnd).ApplyTo(Dockable.GameObject.transform);
                    if (Status == TugStatus.Docking)
                    {
                        Dockable.EndDocking();
                        Status = TugStatus.WaitingForBayDoorClose;
                        Owner.SignalDockingDone(this, TransitionToLoaded);
                    }
                    else
                    {
                        Dockable.EndUndocking();
                        TransitionToFree();
                    }
                }


                break;
        }
    }
}

public enum TugStatus
{
    Docking,
    WaitingForBayDoorClose,
    Docked,
    Undocking,
    UndockedWaitingForTriggerExit
}
