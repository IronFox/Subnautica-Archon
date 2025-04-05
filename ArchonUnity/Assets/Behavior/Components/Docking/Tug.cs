using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Tug : MonoBehaviour
{
    public BayControl Owner { get; private set; }
    public TugStatus Status { get; private set; }
    public bool IsSaving {get; private set; }

    public static string Tag { get; } = $"Archon Docked "+new Guid("086EA558-170A-4B92-8922-F7456F818D38");

    public bool HasGoodFit => fit.Dockable != null && Status != TugStatus.Undefined;
    public DockingFit Fit 
    {
        get
        {
            if (fit.Dockable is null)
                throw new NullReferenceException($"Tug.Fit has not been assigned");
            return fit;
        }
        private set
        {
            if (value.Dockable is null)
                throw new ArgumentNullException($"Trying to assing invalid fit");
            fit = value;
        }
    }
    private DockingFit fit;
    private LogConfig Log { get; set; } = LogConfig.Default;
    
    private float WaitSeconds { get; set; }
    private Undoable UndoTugging { get; } = new Undoable();
    private Undoable ParticleSystems {get; } = new Undoable();
    private Undoable Renderers { get; } = new Undoable();
    private Undoable Lights { get; } = new Undoable();
    private Undoable DisabledBehavioursOnBayDoorCloseWait { get; } = new Undoable();
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

    private Location DockedLocation => Fit.CorrectDocked(Location.FromLocal(Owner.dockedBounds));
    private int ReDisable { get; set; }
    internal void Bind(BayControl bayControl, DockingFit fit, TugStatus status)
    {
        Log = new LogConfig($"Tug[{GetInstanceID()}]<{fit.GameObject.NiceName()}>", true);

        Owner = bayControl;
        Status = status;
        Fit = fit;

        if (status != TugStatus.UndockedWaitingForTriggerExit)
            fit.GameObject.transform.SetParent(Owner.dockedSubRoot);

        switch (status)
        {
            case TugStatus.WaitingForBayDoorOpen:
                foreach (var o in fit.Dockable.GetAllObjects())
                    try
                    {
                        o.RequireActive();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                break;
            case TugStatus.Docking:
                Do(fit.Dockable.BeginDocking, $"Dockable.BeginDocking()", verifyIntegrity:false);
                break;
            case TugStatus.Docked:
                DockedLocation.ApplyTo(Fit.GameObject.transform);

                Do(fit.Dockable.RestoreDockedStateFromSaveGame, $"Dockable.RestoreDockedStateFromSaveGame()", verifyIntegrity: false);
                ChangeActiveState(false);
                Fit.Dockable.DisableAllEnabledRenderers(Renderers);
                Fit.Dockable.DisableAllEnabledLights(Lights);
                Fit.Dockable.DisableAllActiveParticleEmitters(ParticleSystems);
                ReDisable = 3;
                DockedLocation.ApplyTo(Fit.GameObject.transform);

                foreach (var o in fit.Dockable.GetAllObjects())
                    try
                    {
                        o.RequireActive();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                //Fit.GetAllComponents<MonoBehaviour>()
                //    .Where(x => x != this)
                //    .ToEnabled()
                //    .DisableAllEnabled(DisabledBehavioursOnBayDoorCloseWait);

                break;
            case TugStatus.Undocking:
                Do(fit.Dockable.BeginUndocking, $"Dockable.BeginUndocking()", verifyIntegrity: false);
                break;
        }
        fit.Dockable.DisableAllEnabledColliders(UndoTugging, forced:true);
        fit.Dockable.DisableRigidbodies(UndoTugging, forced: true);



        switch (status)
        {
            case TugStatus.WaitingForBayDoorOpen:

                ChangeActiveState(true);

                if (Fit.Dockable.ShouldUnfreezeImmediately)
                    DisabledBehavioursOnBayDoorCloseWait.UndoAndClear();
                Renderers.UndoAndClear();
                Lights.UndoAndClear();

                AnimationStart = DockedLocation;
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
                AnimationEnd = () => DockedLocation;
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

        Fit.GameObject.transform.SetParent(Owner.archon.transform.parent);

        UndoTugging.UndoAndClear();
        Renderers.UndoAndClear();
        Lights.UndoAndClear();
        ParticleSystems.UndoAndClear();
        DisabledBehavioursOnBayDoorCloseWait.UndoAndClear();

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
            if (Fit.GameObject.transform.parent != Owner.dockedSubRoot)
            {
                Log.LogError($"Dockable resides in wrong parent ({Fit.GameObject.transform.parent.PathToString()}). Moving to {Owner.dockedSubRoot}");
                Fit.GameObject.transform.SetParent(Owner.dockedSubRoot);
            }
        }
        else
        {
            if (Fit.GameObject.transform.IsChildOf(Owner.dockedSubRoot))
            {
                Log.LogError($"{Fit} is still a child of {this}. Offloading");
                Fit.GameObject.transform.SetParent(Owner.archon.transform.parent);
            }
        }
        //ObjectUtil.RequireActive(this, Owner.archon.transform);
        Owner.VerifyIntegrity();

    }

    private void TransitionToDocked()
    {
        Log.Write($"Docked");
        Status = TugStatus.Docked;
        ChangeActiveState(false);


        Fit.Dockable.DisableAllEnabledRenderers(Renderers);
        Fit.Dockable.DisableAllEnabledLights(Lights);
        Fit.Dockable.DisableAllActiveParticleEmitters(ParticleSystems);

        DockedLocation.ApplyTo(Fit.GameObject.transform);


        Do(Fit.Dockable.OnDockingDone, $"Dockable.OnDockingDone()");
    }

    private void TransitionToWaitingForBayDoorClose()
    {
        Log.Write($"WaitingForBayDoorClose");
        Status = TugStatus.WaitingForBayDoorClose;

        //Fit.GetAllComponents<MonoBehaviour>()
        //        .Where(x => x != this)
        //        .ToEnabled()
        //        .DisableAllEnabled(DisabledBehavioursOnBayDoorCloseWait);
        
        UndoTugging.RedoAll(); //recheck these, seen falling brawn suits

        Do(Fit.Dockable.EndDocking, $"Dockable.EndDocking()");

        Local(AnimationEnd()).ApplyTo(Fit.GameObject.transform);   //just in case
    }


    private void BeginUndocking()
    {
        Log.Write($"Undocking");

        Status = TugStatus.Undocking;

        DisabledBehavioursOnBayDoorCloseWait.UndoAndClear();
        ParticleSystems.UndoAndClear();
        Renderers.UndoAndClear();
        Lights.UndoAndClear();


        AnimationStart = DockedLocation;
        AnimationStart.ApplyTo(Fit.GameObject);
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

    public void PrepareForSaving()
    {
        Log.Write(nameof(PrepareForSaving));
        IsSaving = true;

        DisabledBehavioursOnBayDoorCloseWait.UndoAll();
        UndoTugging.UndoAll();
        ParticleSystems.UndoAll();
        Renderers.UndoAll();
        Lights.UndoAll();
        foreach (var o in fit.Dockable.GetAllObjects())
            try
            {
                o.RequireActive();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

        //Fit.Dockable.Tag(Tag);
        //Fit.GameObject.transform.SetParent(Owner.archon.transform.parent);

        //DockedLocation.Globalize(Owner.archon.transform).ApplyTo(Fit.GameObject);

        //Do(Fit.Dockable.OnUndockedForSaving,$"Fit.Dockable.OnUndockedForSaving",false);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsSaving)
        {
            if (Time.deltaTime != 0)
            {
                Log.Write($"Saving assumed done. Reintegrating");

                DisabledBehavioursOnBayDoorCloseWait.RedoAll();
                UndoTugging.RedoAll();
                ParticleSystems.RedoAll();
                Renderers.RedoAll();
                Lights.RedoAll();
                //Fit.GameObject.transform.SetParent(Owner.dockedSubRoot);
                //Fit.Dockable.Untag(Tag);
                //Do(Fit.Dockable.OnRedockedAfterSaving, $"Fit.Dockable.OnRedockedAfterSaving");

                //DockedLocation.ApplyTo(Fit.GameObject);

                IsSaving = false;
            }
            else
            {
                //Log.Write($"Saving assumed to continue");
                return;
            }
        }


        LastUpdate = DateTime.Now;
        CheckIntegrity();
        try
        {
            switch (Status)
            {
                case TugStatus.Docked:
                    if (--ReDisable > 0)
                    {
                        ChangeActiveState(false);
                        Fit.Dockable.DisableAllEnabledRenderers(Renderers);
                        Fit.Dockable.DisableAllEnabledLights(Lights);
                        Fit.Dockable.DisableAllActiveParticleEmitters(ParticleSystems);
                    }
                    break;
                case TugStatus.UndockedWaitingForTriggerExit:
                    WaitSeconds += Time.deltaTime;
                    if (WaitSeconds > 1 && !Owner.dockingTrigger.IsTracked(Fit.GameObject))
                    {
                        Log.Write("No longer in trigger zone. Releasing");

                        Do(Fit.Dockable.OnUndockingDone, $"Dockable.OnUndockingDone()");
                        //if (transform.childCount > 0)
                        //{
                        //    Log.LogError($"Tug should not have children at this point but has {transform.childCount}");
                        //    foreach (var c in transform.GetChildren())
                        //    {
                        //        Log.LogError($"Found [{c}]. Relocating out of tug");
                        //        c.SetParent(Owner.archon.transform.parent);
                        //    }
                        //}

                        Log.Write($"Destroying [{this}]");

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
                            TransitionToFree();
                            Owner.ReleaseActive(this);
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

    public static Tug Get(Transform t)
        => t.GetComponent<Tug>();
    public static Tug GetOrAdd(Transform t)
        => GetOrAdd(t.gameObject);
    public static Tug GetOrAdd(GameObject go)
    {
        var tug = go.GetComponent<Tug>();
        if (!tug)
            tug = go.AddComponent<Tug>();
        return tug;
    }


}

public enum TugStatus
{
    Undefined,
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
        if (dockable is null)
            throw new ArgumentNullException(nameof(dockable));
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