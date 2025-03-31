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
    
    private int WaitCount { get; set; }
    private List<Action> UndoTugging { get; } = new List<Action>();
    private List<Action> UndoDocked {get; } = new List<Action>();
    public TransformDescriptor AnimationStart { get; private set; }
    public TransformDescriptor AnimationEnd { get; private set; }
    public float AnimationSeconds { get; private set; }
    public float AnimationProgress { get; private set; }
    public bool WantsDoorsOpen
    {
        get
        {
            switch (Status)
            {
                case TugStatus.WaitingForBayDoorClose:
                case TugStatus.Docked:
                case TugStatus.UndockedWaitingForTriggerExit:
                    return false;
                case TugStatus.WaitingForBayDoorOpen:
                case TugStatus.Undocking:
                case TugStatus.Docking:
                    return true;
                default:
                    return false;
            }
        }
    }


    internal void Bind(BayControl bayControl, IDockable dockable, TugStatus status)
    {
        Log = new LogConfig($"Tug[{GetInstanceID()}]<{dockable.GameObject}>", true);
        //UndoTugging.Clear();

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
                Log.Write($"Dockable.BeginDocking()");
                dockable.BeginDocking();
                break;
            case TugStatus.Docked:
                Log.Write($"Dockable.BeginDocking()");
                dockable.BeginDocking();
                Log.Write($"Dockable.EndDocking()");
                dockable.EndDocking();
                TransitionToDocked();
                break;
            case TugStatus.Undocking:
                Log.Write($"Dockable.BeginUndocking()");
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
        dockable.GameObject.transform.parent = bayControl.dockedSubRoot;
        dockable.GameObject.transform.position = pos;
        dockable.GameObject.transform.rotation = rot;

        switch (status)
        {
            case TugStatus.Docked:
                TransitionToDocked();
                break;
            case TugStatus.WaitingForBayDoorOpen:
                foreach (var c in UndoDocked)
                    c();
                UndoDocked.Clear();
                Log.Write($"Dockable.PrepareUndocking()");
                Dockable.PrepareUndocking();
                break;
        }

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



    private void TransitionToFree()
    {
        if (Status != TugStatus.Undocking)
            throw new InvalidOperationException($"Cannot transition to free from {Status}");
        Log.Write($"Free");
        Status = TugStatus.UndockedWaitingForTriggerExit;
        WaitCount = 0;

        var pos = Dockable.GameObject.transform.position;
        var rot = Dockable.GameObject.transform.rotation;
        Dockable.GameObject.transform.parent = Owner.archon.transform.parent;
        Dockable.GameObject.transform.position = pos;
        Dockable.GameObject.transform.rotation = rot;


        foreach (var c in UndoTugging)
            c();
        UndoTugging.Clear();

        Log.Write($"Dockable.EndUndocking()");
        Dockable.EndUndocking();
    }

    private void TransitionToDocked()
    {
        Log.Write($"Loaded");
        Status = TugStatus.Docked;
        UndoDocked.Clear();

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

        Log.Write($"Dockable.OnDockingDone()");
        Dockable.OnDockingDone();
        
    }

    private void BeginUndocking()
    {
        Log.Write($"Undocking");

        Status = TugStatus.Undocking;

        foreach (var c in UndoDocked)
            c();
        UndoDocked.Clear();

        Dockable.GameObject.transform.localPosition = Owner.dockedBounds.localPosition;
        Dockable.GameObject.transform.localRotation = Owner.dockedBounds.localRotation;
        AnimationStart = TransformDescriptor.FromLocal(Dockable.GameObject.transform);
        AnimationEnd = TransformDescriptor.FromLocal(Owner.dockingTrigger.transform);
        RestartAnimation();
        Log.Write($"Dockable.BeginUndocking()");
        Dockable.BeginUndocking();
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
                if (++WaitCount > 3 && !Owner.dockingTrigger.IsTracked(Dockable.GameObject))
                {
                    Log.Write("No longer in trigger zone. Releasing");

                    Log.Write($"Dockable.OnUndockingDone()");
                    Dockable.OnUndockingDone();

                    Destroy(this);
                }
                break;
            case TugStatus.WaitingForBayDoorClose:
                if (Owner.DoorsAreClosed)
                {
                    Owner.ReleaseActive(this);
                    Log.Write("Doors closed. Concluding");
                    TransitionToDocked();
                }
                else
                    Dockable.UpdateWaitingForBayDoorClose();
                break;
            case TugStatus.WaitingForBayDoorOpen:
                if (Owner.DoorsAreSufficientlyOpen)
                {
                    Log.Write($"Doors open wide enough. Undocking");
                    BeginUndocking();
                }
                else
                    Dockable.UpdateWaitingForBayDoorOpen();
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

                        Log.Write($"Dockable.EndDocking()");
                        Dockable.EndDocking();
                        Status = TugStatus.WaitingForBayDoorClose;
                    }
                    else
                    {
                        Owner.ReleaseActive(this);
                        TransitionToFree();
                    }
                }


                break;
        }
    }

    public static Tug GetOf(Transform t)
        => GetOf(t.gameObject);
    public static Tug GetOf(GameObject go)
    {
        var tug = go.GetComponent<Tug>();
        if (!tug)
            tug = go.AddComponent<Tug>();
        return tug;
    }

}

public enum TugStatus
{
    Docking,
    WaitingForBayDoorClose,
    Docked,
    WaitingForBayDoorOpen,
    Undocking,
    UndockedWaitingForTriggerExit
}
