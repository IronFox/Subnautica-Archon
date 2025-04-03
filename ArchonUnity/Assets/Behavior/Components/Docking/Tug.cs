using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Tug : MonoBehaviour
{
    public BayControl Owner { get; private set; }
    public TugStatus Status { get; private set; }
    public IDockable Dockable { get; private set; }

    private LogConfig Log { get; set; } = LogConfig.Default;
    
    private float WaitSeconds { get; set; }
    private Undoable UndoTugging { get; } = new Undoable();
    private Undoable ParticleSystems {get; } = new Undoable();
    private Undoable Renderers { get; } = new Undoable();
    private Undoable Lights { get; } = new Undoable();
    private Undoable Behaviours { get; } = new Undoable();
    public Location AnimationStart { get; private set; }
    public Func<Location> AnimationEnd { get; private set; }
    public float AnimationSeconds { get; private set; }
    public float AnimationProgress { get; private set; }
    public Vector3 Correction { get; private set; }

    
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


    private DateTime LastUpdate { get; set; }
    public override string ToString()
        => $"Tug[{GetInstanceID()}]<{Dockable}>{{{Status}/{AnimationProgress.ToStr()}/o={Owner.DoorOpenStatus.ToStr()}/{Owner.DoorsAreClosed}/{DateTime.Now - LastUpdate}/e={isActiveAndEnabled}/oe={Owner.isActiveAndEnabled}}}";

    private void Do(Action action, string actionDesc, bool verifyIntegrity=true, bool logAction=true)
    {
        if (verifyIntegrity)
            CheckIntegrity();
        try
        {
            if (logAction)
                Log.Write(actionDesc);
            action();
        }
        catch (Exception ex)
        {
            Log.LogException(actionDesc, ex);
        }
        if (verifyIntegrity)
            CheckIntegrity();

    }
    internal void Bind(BayControl bayControl, IDockable dockable, TugStatus status)
    {
        Log = new LogConfig($"Tug[{GetInstanceID()}]<{dockable.GameObject.NiceName()}>", true);
        //UndoTugging.Clear();
        Correction = -dockable.LocalBounds.center;

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
                Do(dockable.BeginDocking, $"Dockable.BeginDocking()", verifyIntegrity:false);
                break;
            case TugStatus.Docked:
                Do(dockable.RestoreDockedStateFromSaveGame, $"Dockable.RestoreDockedStateFromSaveGame()", verifyIntegrity: false);
                TransitionToDocked();
                break;
            case TugStatus.Undocking:
                Do(dockable.BeginUndocking, $"Dockable.BeginUndocking()", verifyIntegrity: false);
                break;
        }
        dockable.DisableAllEnabledColliders(UndoTugging);
        dockable.DisableRigidbodies(UndoTugging);

        if (status != TugStatus.UndockedWaitingForTriggerExit)
            dockable.GameObject.transform.SetParent(transform);

        switch (status)
        {
            case TugStatus.WaitingForBayDoorOpen:

                ChangeActiveState(true);

                if (Dockable.ShouldUnfreezeImmediately)
                    Behaviours.UndoAndClear();
                Renderers.UndoAndClear();
                Lights.UndoAndClear();

                CheckIntegrity();
                Do(Dockable.PrepareUndocking, $"Dockable.PrepareUndocking()");
                break;
            case TugStatus.Undocking:
                BeginUndocking();
                break;
            default:
                AnimationStart = Location.FromGlobal(dockable.GameObject.transform);
                AnimationEnd = () => 
                Location
                    .FromLocal(Owner.dockedBounds.transform)
                    .TranslatedBy(Correction);
                RestartAnimation();
                break;
        }
        CheckIntegrity();
    }



    private void TransitionToFree()
    {
        if (Status != TugStatus.Undocking)
            throw new InvalidOperationException($"Cannot transition to free from {Status}");
        Log.Write($"Free");
        Status = TugStatus.UndockedWaitingForTriggerExit;
        WaitSeconds = 0;

        transform.GetChildren().ForEach( child => child.SetParent(Owner.archon.transform.parent));

        UndoTugging.UndoAndClear();
        Renderers.UndoAndClear();
        Lights.UndoAndClear();
        ParticleSystems.UndoAndClear();
        Behaviours.UndoAndClear();

        foreach (var body in Dockable.GetAllComponents<Rigidbody>())
        {
            var v = Owner.archon.GetComponent<Rigidbody>().velocity;
            body.velocity = v;
            Log.Write($"Forwarded velocity {v} to [{body}] of {Dockable}");
        }

        Do(Dockable.EndUndocking, $"Dockable.EndUndocking()");
    }

    private void ChangeActiveState(bool active)
    {
        if (Dockable.GameObject.activeSelf != active)
        {
            Dockable.GameObject.SetActive(active);
            Log.Write($"Active:={Dockable.GameObject.activeSelf}");
        }
    }

    public void CheckIntegrity()
    {
        if (Status != TugStatus.UndockedWaitingForTriggerExit)
        {
            if (Dockable.GameObject.transform.parent != transform)
            {
                Log.LogError($"Dockable resides in wrong parent ({Dockable.GameObject.transform.parent.PathToString()}). Moving to {transform}");
                transform.GetChildren().ForEach(child => child.SetParent(Owner.archon.transform.parent));
                Dockable.GameObject.transform.SetParent (transform);
            }
        }
        else
        {
            if (Dockable.GameObject.transform.IsChildOf(transform))
            {
                Log.LogError($"{Dockable} is still a child of {this}. Offloading");
                Dockable.GameObject.transform.SetParent(Owner.archon.transform.parent);
            }
        }
        ObjectUtil.RequireActive(this, Owner.archon.transform);

    }

    private void TransitionToDocked()
    {
        Log.Write($"Loaded");
        Status = TugStatus.Docked;



        ChangeActiveState(false);


        Dockable.DisableAllEnabledRenderers(Renderers);
        Dockable.DisableAllEnabledLights(Lights);
        Dockable.DisableAllActiveParticleEmitters(ParticleSystems);


        Location.FromLocal(Owner.dockedBounds.transform)
            .TranslatedBy(Correction)
            .ApplyTo(Dockable.GameObject.transform);


        Do(Dockable.OnDockingDone, $"Dockable.OnDockingDone()");
    }

    private void TransitionToWaitingForBayDoorClose()
    {
        Log.Write($"WaitingForBayDoorClose");
        Status = TugStatus.WaitingForBayDoorClose;

        Dockable.GetAllComponents<MonoBehaviour>()
                .Where(x => !x == this)
                .ToEnabled()
                .DisableAllEnabled(Behaviours);
        
        UndoTugging.RedoAll(); //recheck these, seen falling brawn suits

        Do(Dockable.EndDocking, $"Dockable.EndDocking()");

        Local(AnimationEnd()).ApplyTo(Dockable.GameObject.transform);   //just in case
    }


    private void BeginUndocking()
    {
        Log.Write($"Undocking");

        Status = TugStatus.Undocking;

        Behaviours.UndoAndClear();
        ParticleSystems.UndoAndClear();
        Renderers.UndoAndClear();
        Lights.UndoAndClear();


        AnimationStart = Location
            .FromLocal(Owner.dockedBounds)
            .TranslatedBy(Correction);
        AnimationStart
            .ApplyTo(Dockable.GameObject);
        AnimationEnd = () =>
        {
            var td = Location.FromLocal(Owner.dockingTrigger.transform);
            if (Dockable.UndockUpright)
                td = td.WithGlobalRotation(Owner.transform, Quaternion.Euler(0, Owner.dockingTrigger.transform.eulerAngles.y, 0));
            return td;
        };

        RestartAnimation();
        Do(Dockable.BeginUndocking, $"Dockable.BeginUndocking()");
    }

    private Vector3 LocalPosition(Location desc)
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

    private Location Local(Location desc)
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
    

    private Location Global(Location desc)
    {
        switch (desc.Locality)
        {
            case TransformLocality.Global:
                return desc;
            case TransformLocality.Local:
                return desc.Globalize(Owner.transform);
            default:
                return desc;
        }
    }

    private void RestartAnimation()
    {
        AnimationProgress = 0;
        AnimationSeconds = M.Distance(LocalPosition(AnimationStart), LocalPosition(AnimationEnd())) / Owner.dockingMetersPerSecond;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        LastUpdate = DateTime.Now;
        CheckIntegrity();
        try
        {
            switch (Status)
            {
                case TugStatus.UndockedWaitingForTriggerExit:
                    WaitSeconds += Time.deltaTime;
                    if (WaitSeconds > 1 && !Owner.dockingTrigger.IsTracked(Dockable.GameObject))
                    {
                        Log.Write("No longer in trigger zone. Releasing");

                        Do(Dockable.OnUndockingDone, $"Dockable.OnUndockingDone()");
                        if (transform.childCount > 0)
                        {
                            Log.LogError($"Tug should not have children at this point but has {transform.childCount}");
                            foreach (var c in transform.GetChildren())
                            {
                                Log.LogError($"Found [{c}]. Relocating out of tug");
                                c.SetParent(Owner.archon.transform.parent);
                            }
                        }

                        Log.Write($"Destroying [{gameObject}]");

                        Destroy(gameObject);
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
                    {
                        if (UndoTugging.RedoAll()
                            || 
                            ( Dockable.DisableAllEnabledColliders(UndoTugging)
                            | Dockable.DisableRigidbodies(UndoTugging))
                            )
                        {
                            Local(AnimationEnd())
                                .ApplyTo(Dockable.GameObject.transform);
                        }

                        Do(Dockable.UpdateWaitingForBayDoorClose, "Dockable.UpdateWaitingForBayDoorClose()", logAction: false);
                    }
                    break;
                case TugStatus.WaitingForBayDoorOpen:
                    if (Owner.DoorsAreSufficientlyOpen)
                    {
                        Log.Write($"Doors open wide enough. Undocking");
                        BeginUndocking();
                    }
                    else
                        Do(Dockable.UpdateWaitingForBayDoorOpen, "Dockable.UpdateWaitingForBayDoorOpen()", logAction: false);
                    break;
                case TugStatus.Docking:
                case TugStatus.Undocking:
                    AnimationProgress += Time.deltaTime / AnimationSeconds;
                    if (AnimationProgress < 1)
                    {
                        Location
                            .Lerp(
                                Local(AnimationStart),
                                Local(AnimationEnd()),
                                M.Smooth(AnimationProgress))
                            .ApplyTo(Dockable.GameObject.transform);
                    }
                    else
                    {
                        Log.Write($"Animation end reached");
                        Local(AnimationEnd())
                            .ApplyTo(Dockable.GameObject.transform);
                        if (Status == TugStatus.Docking)
                        {

                            TransitionToWaitingForBayDoorClose();
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
        catch (Exception e)
        {
            Log.LogException(e);
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
