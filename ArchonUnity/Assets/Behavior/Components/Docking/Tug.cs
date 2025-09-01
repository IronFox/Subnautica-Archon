using Assets.Behavior.Adapters;
using Assets.Behavior.Util;
using Behavior.Util.Math;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Behavior.Components.Docking
{

    public class Tug : MonoBehaviour
    {
        public BayControl Owner { get; private set; }
        public TugStatus Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                    return;
                _status = value;
                Owner.archon.SignalDockedChange();
            }
        }
        private TugStatus _status;
        public bool IsSaving { get; private set; }

        public static string Tag { get; } = $"Archon Docked " + new Guid("086EA558-170A-4B92-8922-F7456F818D38");

        public bool HasGoodFit => fit.Dockable != null;
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

        private float WaitSeconds { get; set; }
        private Util.Undoable.UndoableActions UndoTugging { get; } = new Util.Undoable.UndoableActions();
        private Util.Undoable.UndoableActions ParticleSystems { get; } = new Util.Undoable.UndoableActions();
        private Util.Undoable.UndoableActions Renderers { get; } = new Util.Undoable.UndoableActions();
        private Util.Undoable.UndoableActions Lights { get; } = new Util.Undoable.UndoableActions();
        private Util.Undoable.UndoableActions DisabledBehavioursOnBayDoorCloseWait { get; } = new Util.Undoable.UndoableActions();
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
                    case TugStatus.DockingWaitingForBayDoorClose:
                    case TugStatus.Docked:
                    case TugStatus.UndockedWaitingForTriggerExit:
                        return false;
                    case TugStatus.UndockingWaitingForBayDoorOpen:
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


        private void Do(Action action, string actionDesc, bool verifyIntegrity = true, bool logAction = true)
        {
            if (verifyIntegrity)
                CheckIntegrity();
            try
            {
                if (logAction)
                    using (var log = Log.New())
                        log.Write(actionDesc);
                action();
            }
            catch (Exception ex)
            {
                using (var log = Log.New())
                    log.Error(actionDesc, ex);
            }
            if (verifyIntegrity)
                CheckIntegrity();
        }

        private Location DockedLocation => Fit.CorrectDocked(Location.FromLocal(Owner.dockedBounds));
        private Location ParkLocation => Fit.CorrectDocked(Location.FromLocal(Owner.parkPostion));
        private int ReDisable { get; set; }

        //private ILogAdapter NewLog() =>
        //   Log.New($"Tug#{GetInstanceID()}", fit.GameObject.NiceName());

        internal void Bind(BayControl bayControl, DockingFit fit, TugStatus status)
        {
            using (var log = Log.New())
            {
                log.Write($"Binding with status {status} and bounds {fit.Bounds}");
                Owner = bayControl;
                Status = status;
                Fit = fit;

                if (status != TugStatus.UndockedWaitingForTriggerExit)
                    fit.GameObject.transform.SetParent(Owner.dockedSubRoot);

                switch (status)
                {
                    case TugStatus.Docking:
                        Do(fit.Dockable.BeginDocking, $"Dockable.BeginDocking()", verifyIntegrity: false);
                        break;
                    case TugStatus.Docked:
                        DockedLocation.ApplyTo(Fit.GameObject.transform);

                        Do(fit.Dockable.RestoreDockedStateFromSaveGame, $"Dockable.RestoreDockedStateFromSaveGame()", verifyIntegrity: false);
                        ChangeActiveState(false);
                        Fit.Dockable.ForceDisableAllRenderers(Renderers);
                        Fit.Dockable.DisableAllEnabledCanvases(Renderers);
                        Fit.Dockable.DisableAllEnabledLights(Lights);
                        Fit.Dockable.DisableAllActiveParticleEmitters(ParticleSystems);
                        ReDisable = 100;
                        ParkLocation.ApplyTo(Fit.GameObject.transform);

                        //Fit.GetAllComponents<MonoBehaviour>()
                        //    .Where(x => x != this)
                        //    .ToEnabled()
                        //    .DisableAllEnabled(DisabledBehavioursOnBayDoorCloseWait);

                        break;
                    case TugStatus.Undocking:
                        Do(fit.Dockable.BeginUndocking, $"Dockable.BeginUndocking()", verifyIntegrity: false);
                        break;
                }
                fit.Dockable.DisableAllEnabledColliders(UndoTugging /*, forced: true*/);
                fit.Dockable.DisableRigidbodies(UndoTugging, forced: true);



                switch (status)
                {
                    case TugStatus.UndockingWaitingForBayDoorOpen:

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
        }



        private void TransitionToFree()
        {
            if (Status != TugStatus.Undocking)
                throw new InvalidOperationException($"Cannot transition to free from {Status}");
            using (var log = Log.New())
            {
                log.Write($"Free");
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
                    log.Write($"Forwarded velocity {v} to [{body}] of {Fit}");
                }

                Do(Fit.Dockable.EndUndocking, $"Dockable.EndUndocking()");
            }
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
            {
                if (Status != TugStatus.UndockedWaitingForTriggerExit)
                {
                    if (Fit.GameObject.transform.parent != Owner.dockedSubRoot)
                    {
                        using (var log = Log.New())
                            log.Error($"Dockable resides in wrong parent ({Fit.GameObject.transform.parent.PathToString()}). Moving to {Owner.dockedSubRoot}");
                        Fit.GameObject.transform.SetParent(Owner.dockedSubRoot);
                    }
                }
                else
                {
                    if (Fit.GameObject.transform.IsChildOf(Owner.dockedSubRoot))
                    {
                        using (var log = Log.New())
                            log.Error($"{Fit} is still a child of {this}. Offloading");
                        Fit.GameObject.transform.SetParent(Owner.archon.transform.parent);
                    }
                }
                ObjectUtil.RequireActive(this, Owner.archon.transform);
                Owner.VerifyIntegrity();
            }

        }

        private void TransitionToDocked()
        {
            using (var log = Log.New())
            {

                log.Write($"Docked");
                Status = TugStatus.Docked;
                ChangeActiveState(false);


                Fit.Dockable.DisableAllEnabledRenderers(Renderers);
                Fit.Dockable.DisableAllEnabledCanvases(Renderers);
                Fit.Dockable.DisableAllEnabledLights(Lights);
                Fit.Dockable.DisableAllActiveParticleEmitters(ParticleSystems);

                DockedLocation.ApplyTo(Fit.GameObject.transform);


                Do(Fit.Dockable.OnDockingDone, $"Dockable.OnDockingDone()");

                ParkLocation.ApplyTo(Fit.GameObject.transform);
            }

        }

        private void TransitionToWaitingForBayDoorClose()
        {
            using (var log = Log.New())
            {

                log.Write($"WaitingForBayDoorClose");
                Status = TugStatus.DockingWaitingForBayDoorClose;

                //Fit.GetAllComponents<MonoBehaviour>()
                //        .Where(x => x != this)
                //        .ToEnabled()
                //        .DisableAllEnabled(DisabledBehavioursOnBayDoorCloseWait);

                UndoTugging.RedoAll(); //recheck these, seen falling brawn suits

                Do(Fit.Dockable.EndDocking, $"Dockable.EndDocking()");

                Local(AnimationEnd()).ApplyTo(Fit.GameObject.transform);   //just in case
            }
        }


        private void BeginUndocking()
        {
            using (var log = Log.New())
            {
                log.Write($"Undocking");

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


        public void PrepareForSaving()
        {
            using (var log = Log.New())
            {
                log.Write(nameof(PrepareForSaving));
                IsSaving = true;

                DisabledBehavioursOnBayDoorCloseWait.UndoAll();
                UndoTugging.UndoAll();
                ParticleSystems.UndoAll();
                //Renderers.UndoAll();
                Lights.UndoAll();
                //Fit.Dockable.Tag(Tag);
                Fit.GameObject.transform.SetParent(Owner.archon.transform.parent);

                if (Status == TugStatus.Docked)
                    ParkLocation
                        .Globalize(Owner.archon.transform)
                        .ApplyTo(Fit.GameObject);

                Do(Fit.Dockable.OnUndockedForSaving, $"Fit.Dockable.OnUndockedForSaving", false);
            }
        }

        // Update is called once per frame
        void Update()
        {
            {
                if (IsSaving)
                {
                    if (Time.deltaTime != 0)
                    {
                        using (var log = Log.New())
                        {
                            log.Write($"Saving assumed done. Reintegrating");

                            DisabledBehavioursOnBayDoorCloseWait.RedoAll();
                            UndoTugging.RedoAll();
                            ParticleSystems.RedoAll();
                            Renderers.RedoAll();
                            Lights.RedoAll();
                            Fit.GameObject.transform.SetParent(Owner.dockedSubRoot);
                            //Fit.Dockable.Untag(Tag);
                            Do(Fit.Dockable.OnRedockedAfterSaving, $"Fit.Dockable.OnRedockedAfterSaving");
                            ReDisable = 100;
                            DockedLocation.ApplyTo(Fit.GameObject);

                            IsSaving = false;
                        }
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
                                Fit.Dockable.DisableAllEnabledCanvases(Renderers);
                                Fit.Dockable.DisableAllEnabledLights(Lights);
                                Fit.Dockable.DisableAllActiveParticleEmitters(ParticleSystems);
                                ParkLocation.ApplyTo(Fit.GameObject.transform);
                                // ReDisable = 10;
                            }
                            break;
                        case TugStatus.UndockedWaitingForTriggerExit:
                            WaitSeconds += Time.deltaTime;
                            if (WaitSeconds > 1 && !Owner.dockingTrigger.IsTracked(Fit.GameObject))
                            {
                                using (var log = Log.New())
                                {
                                    log.Write("No longer in trigger zone. Releasing");

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

                                    log.Write($"Destroying [{this}]");

                                    Destroy(this);
                                }
                            }
                            break;
                        case TugStatus.DockingWaitingForBayDoorClose:
                            if (Owner.DoorsAreClosed)
                            {
                                using (var log = Log.New())
                                {
                                    Owner.ReleaseActive(this);
                                    log.Write("Doors closed. Concluding");
                                    TransitionToDocked();
                                }
                            }
                            else
                            {
                                UndoTugging.RedoAll();
                                Local(AnimationEnd()).ApplyTo(Fit.GameObject.transform);   //just in case
                                Do(Fit.Dockable.UpdateWaitingForBayDoorClose, "Dockable.UpdateWaitingForBayDoorClose()", logAction: false);
                            }
                            break;
                        case TugStatus.UndockingWaitingForBayDoorOpen:
                            if (Owner.DoorsAreSufficientlyOpen)
                            {
                                using (var log = Log.New())
                                {
                                    log.Write($"Doors open wide enough. Undocking");
                                    BeginUndocking();
                                }
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
                                using (var log = Log.New())
                                {
                                    log.Write($"Animation end reached");
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
                            }


                            break;
                    }
                }
                catch (Exception e)
                {
                    using (var log = Log.New())
                        log.Error($"Caught exception", e);
                }
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
        Docking,
        DockingWaitingForBayDoorClose,
        Docked,
        UndockingWaitingForBayDoorOpen,
        Undocking,
        UndockedWaitingForTriggerExit
    }


    public readonly struct DockingFit
    {
        public IDockable Dockable { get; }
        public Quaternion Rotation { get; }
        public Vector3 CenterCorrection { get; }
        public Bounds3 Bounds { get; }
        public GameObject GameObject => Dockable?.GameObject;

        public DockingFit(IDockable dockable, Quaternion rotation, Bounds3 bounds)
        {
            Dockable = dockable ?? throw new ArgumentNullException(nameof(dockable));
            Rotation = rotation;
            CenterCorrection = -bounds.Center;
            Bounds = bounds;
        }

        public IEnumerable<T> GetAllComponents<T>() where T : Component
            => Dockable.GetAllComponents<T>();

        public Location CorrectDocked(Location location)
            => location
                .RotatedBy(Rotation)
                .TranslatedBy(CenterCorrection);
    }
}