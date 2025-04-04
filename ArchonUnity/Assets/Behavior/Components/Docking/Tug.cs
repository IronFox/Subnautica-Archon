using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Tug : MonoBehaviour
{
    public BayControl Owner { get; private set; }
    public TugStatus Status { get; private set; }
    public DockingFit Fit { get; private set; }

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
        => $"Tug[{GetInstanceID()}]<{Fit}>{{{Status}/{AnimationProgress.ToStr()}/o={Owner.DoorOpenStatus.ToStr()}/{Owner.DoorsAreClosed}/{DateTime.Now - LastUpdate}/e={isActiveAndEnabled}/oe={Owner.isActiveAndEnabled}}}";

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
    internal void Bind(BayControl bayControl, DockingFit fit, TugStatus status)
    {
        Log = new LogConfig($"Tug[{GetInstanceID()}]<{fit.GameObject.NiceName()}>", true);

        Owner = bayControl;
        Status = status;
        Fit = fit;

        switch (status)
        {
            case TugStatus.Docking:
                Do(fit.Dockable.BeginDocking, $"Dockable.BeginDocking()", verifyIntegrity:false);
                break;
            case TugStatus.Docked:
                Do(fit.Dockable.RestoreDockedStateFromSaveGame, $"Dockable.RestoreDockedStateFromSaveGame()", verifyIntegrity: false);
                TransitionToDocked();
                break;
            case TugStatus.Undocking:
                Do(fit.Dockable.BeginUndocking, $"Dockable.BeginUndocking()", verifyIntegrity: false);
                break;
        }
        fit.Dockable.DisableAllEnabledColliders(UndoTugging, forced:true);
        fit.Dockable.DisableRigidbodies(UndoTugging, forced: true);

        if (status != TugStatus.UndockedWaitingForTriggerExit)
            fit.Dockable.GameObject.transform.SetParent(transform);

        switch (status)
        {
            case TugStatus.WaitingForBayDoorOpen:

                ChangeActiveState(true);

                if (Fit.Dockable.ShouldUnfreezeImmediately)
                    Behaviours.UndoAndClear();
                Renderers.UndoAndClear();
                Lights.UndoAndClear();

                AnimationStart = Fit.CorrectDocked(Location.FromGlobal(Owner.dockedBounds.transform));
                AnimationEnd = () => AnimationStart;
                CheckIntegrity();
                Local(AnimationStart).ApplyTo(Fit.GameObject.transform);   //just in case
                Do(Fit.Dockable.PrepareUndocking, $"Dockable.PrepareUndocking()");
                Local(AnimationStart).ApplyTo(Fit.GameObject.transform);   //just in case
                break;
            case TugStatus.Undocking:
                BeginUndocking();
                break;
            default:
                AnimationStart = Location.FromGlobal(fit.Dockable.GameObject.transform);
                AnimationEnd = () =>
                    Fit.CorrectDocked(
                        Location.FromLocal(Owner.dockedBounds.transform)
                    );
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

        foreach (var body in Fit.GetAllComponents<Rigidbody>())
        {
            var v = Owner.archon.GetComponent<Rigidbody>().velocity;
            body.velocity = v;
            Log.Write($"Forwarded velocity {v} to [{body}] of {Fit}");
        }

        Do(Fit.Dockable.EndUndocking, $"Dockable.EndUndocking()");
    }

    private void ChangeActiveState(bool active)
    {
        //if (Dockable.GameObject.activeSelf != active)
        //{
        //    Dockable.GameObject.SetActive(active);
        //    Log.Write($"Active:={Dockable.GameObject.activeSelf}");
        //}
    }

    public void CheckIntegrity()
    {
        if (Status != TugStatus.UndockedWaitingForTriggerExit)
        {
            if (Fit.GameObject.transform.parent != transform)
            {
                Log.LogError($"Dockable resides in wrong parent ({Fit.GameObject.transform.parent.PathToString()}). Moving to {transform}");
                transform.GetChildren().ForEach(child => child.SetParent(Owner.archon.transform.parent));
                Fit.GameObject.transform.SetParent (transform);
            }
        }
        else
        {
            if (Fit.GameObject.transform.IsChildOf(transform))
            {
                Log.LogError($"{Fit} is still a child of {this}. Offloading");
                Fit.GameObject.transform.SetParent(Owner.archon.transform.parent);
            }
        }
        ObjectUtil.RequireActive(this, Owner.archon.transform);

    }

    private void TransitionToDocked()
    {
        Log.Write($"Loaded");
        Status = TugStatus.Docked;



        ChangeActiveState(false);


        Fit.Dockable.DisableAllEnabledRenderers(Renderers);
        Fit.Dockable.DisableAllEnabledLights(Lights);
        Fit.Dockable.DisableAllActiveParticleEmitters(ParticleSystems);

        Fit.CorrectDocked(
            Location.FromLocal(Owner.dockedBounds.transform)
        ).ApplyTo(Fit.GameObject.transform);


        Do(Fit.Dockable.OnDockingDone, $"Dockable.OnDockingDone()");
    }

    private void TransitionToWaitingForBayDoorClose()
    {
        Log.Write($"WaitingForBayDoorClose");
        Status = TugStatus.WaitingForBayDoorClose;

        Fit.GetAllComponents<MonoBehaviour>()
                .Where(x => x != this)
                .ToEnabled()
                .DisableAllEnabled(Behaviours);
        
        UndoTugging.RedoAll(); //recheck these, seen falling brawn suits

        Do(Fit.Dockable.EndDocking, $"Dockable.EndDocking()");

        Local(AnimationEnd()).ApplyTo(Fit.GameObject.transform);   //just in case
    }


    private void BeginUndocking()
    {
        Log.Write($"Undocking");

        Status = TugStatus.Undocking;

        Behaviours.UndoAndClear();
        ParticleSystems.UndoAndClear();
        Renderers.UndoAndClear();
        Lights.UndoAndClear();


        AnimationStart = Fit.CorrectDocked(Location
            .FromLocal(Owner.dockedBounds)
            );
        AnimationStart
            .ApplyTo(Fit.GameObject);
        AnimationEnd = () =>
        {
            var td = Location.FromLocal(Owner.dockingTrigger.transform);
            if (Fit.Dockable.UndockUpright)
                td = td.WithGlobalRotation(Owner.transform, Quaternion.Euler(0, Owner.dockingTrigger.transform.eulerAngles.y, 0));
            return td;
        };

        RestartAnimation();
        Do(Fit.Dockable.BeginUndocking, $"Dockable.BeginUndocking()");
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
                    if (WaitSeconds > 1 && !Owner.dockingTrigger.IsTracked(Fit.GameObject))
                    {
                        Log.Write("No longer in trigger zone. Releasing");

                        Do(Fit.Dockable.OnUndockingDone, $"Dockable.OnUndockingDone()");
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
                        if (UndoTugging.RedoAll())
                        {
                            Local(AnimationEnd())
                                .ApplyTo(Fit.GameObject.transform);
                        }

                        Do(Fit.Dockable.UpdateWaitingForBayDoorClose, "Dockable.UpdateWaitingForBayDoorClose()", logAction: false);
                    }
                    break;
                case TugStatus.WaitingForBayDoorOpen:
                    if (Owner.DoorsAreSufficientlyOpen)
                    {
                        Log.Write($"Doors open wide enough. Undocking");
                        BeginUndocking();
                    }
                    else
                    {
                        Do(Fit.Dockable.UpdateWaitingForBayDoorOpen, "Dockable.UpdateWaitingForBayDoorOpen()", logAction: false);
                        UndoTugging.RedoAll();
                        Local(AnimationStart)
                            .ApplyTo(Fit.GameObject.transform);
                    }
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
                            .ApplyTo(Fit.GameObject.transform);
                    }
                    else
                    {
                        Log.Write($"Animation end reached");
                        Local(AnimationEnd())
                            .ApplyTo(Fit.GameObject.transform);
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


public readonly struct DockingFit
{
    public IDockable Dockable { get; }
    public Quaternion Rotation { get; }
    public Vector3 CenterCorrection { get; }
    public Bounds Bounds { get; }
    public GameObject GameObject => Dockable.GameObject;

    public DockingFit(IDockable dockable, Quaternion rotation, Vector3 centerCorrection, Bounds bounds)
    {
        Dockable = dockable;
        Rotation = rotation;
        CenterCorrection = centerCorrection;
        Bounds = bounds;
    }

    public IEnumerable<T> GetAllComponents<T>() where T:Component
        => Dockable.GetAllComponents<T>();

    public Location CorrectDocked(Location location)
        => location
            .RotatedBy(Rotation)
            .TranslatedBy(CenterCorrection);
}