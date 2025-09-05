using Assets.Behavior.Adapters;
using Assets.Behavior.Util;
using Assets.Behavior.Util.Math;
using Behavior.Util.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Behavior.Components.Docking
{

    public class BayControl : MonoBehaviour
    {
        public float secondsToOpen = 3;
        //public bool open;
        private float progress = 0;
        private Animation openAnimation;

        public TriggerTracker dockingTrigger;
        public SphereCollider minimalFreeUndockSpace;
        private Tug active;

        public float dockingMetersPerSecond = 10;

        public Transform insides;
        public ArchonControl archon;
        public Transform dockedSubRoot;
        public Transform dockedBounds;
        public Transform dockingColliders;
        public Transform parkPostion;
        public SoundAdapter bayDoorSlideSound;
        public SoundAdapter bayDoorLockSound;
        public SoundAdapter bayDoorUnlockSound;

        private float initialBayDoorSlideSoundVolume = 1;


        public int maxDockedVehicles = 2;

        private Bounds3 permittedBounds;
        private bool isLoading;

        public static Action<ArchonControl, IDockable> OnDockingFailedFull { get; set; }
        public static Action<ArchonControl, IDockable> OnDockingFailedTooLarge { get; set; }


        private bool TugFromDocked(GameObject dockedSub, bool destroyIfInvalid, out Tug tug, out IDockable dockable, out UndockingCheckResult undockCheckResult)
            => TugFromGameObject(dockedSub.transform, destroyIfInvalid, out tug, out dockable, out undockCheckResult);
        private bool TugFromGameObject(Transform tugCandidate, bool destroyIfInvalid, out Tug tug, out IDockable dockable, out UndockingCheckResult undockCheckResult)
        {
            using (var log = Log.New())
            {
                //if (tugCandidate.childCount != 1)
                //{
                //    Log.LogError($"Tug candidate [{tugCandidate}] has does not have exactly one child (has {tugCandidate.childCount})");
                //    if (destroyIfInvalid)
                //    {
                //        Log.LogError($"Destroying");
                //        Destroy(tugCandidate);
                //    }
                //    tug = null;
                //    dockable = null;
                //    undockCheckResult = UndockingCheckResult.NotDocked;
                //    return false;
                //}
                if (tugCandidate.parent != dockedSubRoot)
                {
                    log.Error($"Tug candidate [{tugCandidate}] resides in wrong parent ([{tugCandidate.parent}], should be [{dockedSubRoot}]).");
                    if (destroyIfInvalid)
                    {
                        log.Error($"Destroying");
                        Destroy(tugCandidate);
                    }
                    tug = null;
                    dockable = null;
                    undockCheckResult = UndockingCheckResult.NotDocked;
                    return false;
                }
                //        var sub = tugCandidate.GetChild(0);
                dockable = DockingAdapter.ToDockable(tugCandidate.gameObject, archon, DockingAdapter.Filter.All);
                if (dockable == null)
                {
                    log.Error($"Tug candidate [{tugCandidate}] failed to convert to dockable. Probably something else");
                    //if (destroyIfInvalid)
                    //{
                    //    Log.LogError($"Destroying");
                    //    Destroy(tugCandidate);
                    //}
                    tug = null;
                    undockCheckResult = UndockingCheckResult.NotDockable;
                    return false;
                }

                tug = Tug.GetOrAdd(tugCandidate);
                //if (!tug)
                //{
                //    Log.LogError($"Tug candidate {tugCandidate} has no tug. Creating");
                //    tug = tugCandidate.gameObject.AddComponent<Tug>();
                //}
                undockCheckResult = UndockingCheckResult.Ok;
                return true;
            }
        }

        void Awake()
        {
            using (var log = Log.New())
            {
                permittedBounds = dockedBounds.ComputeScaledLocalBounds(includeRenderers: false, includeColliders: true, excludeFrom: null);
                initialBayDoorSlideSoundVolume = bayDoorSlideSound ? bayDoorSlideSound.volume : 1;
                RedetectDocked();
            }
        }

        public void SignalLoading()
        {
            using (var log = Log.New())
                isLoading = true;
        }

        public int RedetectDocked()
        {
            using (var log = Log.New())
            {


                //NumDockedVehicles = 0;

                var candidates = Physics.OverlapSphere(archon.transform.position, 1000);
                log.Write($"Checking {candidates.Length} colliders");
                var rbs = candidates.Select(c => c.attachedRigidbody).Where(x => x).Distinct().ToList();
                log.Write($"Down to {rbs.Count} rigidbodies");


                foreach (var candidate in rbs)
                {
                    try
                    {
                        if (!candidate)
                        {
                            log.Write($"Found null candidate");
                            continue;
                        }

                        if (!candidate.transform)
                        {
                            log.Write($"Found candidate with null transform");
                            continue;
                        }

                        if (candidate.transform.IsChildOf(archon.transform))
                        {
                            log.Write($"Found local {candidate.NiceName()} in {candidate.transform.PathToString()}");
                            continue;
                        }
                        //Log.Write($"Now checking {candidate.NiceName()}");

                        var d = DockingAdapter.ToDockable(candidate.gameObject, archon,
                            DockingAdapter.Filter.CurrentlyDockedBySaveGame);
                        if (d != null)
                        {
                            log.Write("Is dockable");
                            var fit = FindBestFit(d);
                            if (fit != null)
                            {
                                log.Write("Fits. Docking");
                                var tug = Tug.GetOrAdd(d.GameObject);
                                tug.Bind(this, fit.Value, TugStatus.Docked);
                                IncNumDockedVehicles(tug);
                            }
                            else
                            {
                                d.GameObject.transform.position += M.V3(50); //evacuate the thing out
                                log.Error("Tagged but does not fit. Translated away");
                            }
                        }
                        // else
                        //     Log.Write("Is not dockable or not docked");
                    }
                    catch (Exception e)
                    {
                        log.Error($"Caught exception", e);
                    }

                }

                archon.SignalDockedChange();
                return NumDockedVehicles;
            }
        }

        private ComponentSet<Tug> DockedTugs { get; } = new ComponentSet<Tug>();

        public IEnumerable<IDockable> Docked =>
            DockedTugs
                .Where(x =>
                x.Status == TugStatus.Docked
                || x.Status == TugStatus.Docking
                || x.Status == TugStatus.DockingWaitingForBayDoorClose)
                .Select(x => x.Fit.Dockable);

        private void IncNumDockedVehicles(Tug tug)
        {
            if (!DockedTugs.Add(tug))
            {
                throw new InvalidOperationException($"Tug {tug.NiceName()} already added");
            }
            NumDockedVehicles++;
            archon.SignalDocked(tug.Fit.Dockable);
        }

        private void DecNumDockedVehicles(Tug tug)
        {
            if (!DockedTugs.Remove(tug))
            {
                throw new InvalidOperationException($"Tug {tug.NiceName()} not found");
            }
            NumDockedVehicles--;
            archon.SignalDockedChange();
        }

        // Start is called before the first frame update
        void Start()
        {
            openAnimation = GetComponent<Animation>();
            SetBayVisible(false);
        }

        private DockingFit? FindBestFit(IDockable d)
        {
            //using (var log = new LogContext(nameof(FindBestFit)))
            {
                var bounds = d.LocalBounds;

                var fit1 = new DockingFit(d, Quaternion.identity, bounds);
                //log.Write($"Testing fit {fit.CenterCorrection}, {fit.Bounds.size}");
                if (!permittedBounds.ContainsCentered(fit1.Bounds))
                {
                    //Log.LogError($"Candidate vehicle {d} is too large unrotated. Rotating ({fit.Bounds} exeeds {permittedBounds})");

                    bounds = Bounds3.CenterBox(bounds.Center, M.V3(bounds.Size.x, bounds.Size.z, bounds.Size.y)); //flip y and z
                    var fit2 = new DockingFit(d, Quaternion.AngleAxis(90, Vector3.right), bounds);
                    //log.Write($"Testing rotated fit {fit.CenterCorrection}, {fit.Bounds.size}");


                    if (!permittedBounds.ContainsCentered(fit2.Bounds))
                    {
                        using (var log = Log.New())
                            log.Error(
                            $"Candidate vehicle {d} is still too large to dock ({fit1.Bounds} and {fit2.Bounds} exceed {permittedBounds})");
                        return null;
                    }
                }

                return fit1;
            }
        }

        private void SetBayVisible(bool visible)
        {
            foreach (var r in insides.GetComponentsInChildren<Renderer>())
                r.enabled = visible;
            foreach (var r in insides.GetComponentsInChildren<Light>())
                r.enabled = visible;
            foreach (var c in dockingColliders.GetComponentsInChildren<Collider>())
                c.enabled = visible;
        }


        public UndockingCheckResult CheckUndocking(GameObject dockedSub)
        {
            ObjectUtil.RequireActive(this, archon.transform);
            if (active)
            {
                using (var log = Log.New())
                    log.Write($"Cannot undock right now. Still busy working on {active}");
                return UndockingCheckResult.Busy;
            }
            if (!dockedSub)
            {
                using (var log = Log.New())
                    log.Error($"Attempted to undock <null> sub");
                return UndockingCheckResult.DoesNotExist;
            }
            if (UndockingIsObstructed())
                return UndockingCheckResult.Obstructed;
            TugFromDocked(dockedSub, false, out var tug, out var dockable, out var checkResult);
            return checkResult;
        }

        private bool UndockingIsObstructed()
        {
            return false;
            //var hits = Physics.OverlapSphere(minimalFreeUndockSpace.transform.position, minimalFreeUndockSpace.radius);
            //foreach (var hit in hits)
            //{
            //    if (
            //        hit.enabled
            //    && !hit.isTrigger
            //    && !hit.transform.IsChildOf(archon.transform)
            //    && (!hit.attachedRigidbody || hit.attachedRigidbody.isKinematic)    //otherwise try to push it away somehow
            //    )
            //    {
            //        Log.LogError($"Undocking space is obstructed by {hit.transform.PathToString()} [{hit.GetInstanceID()}]");
            //        return true;
            //    }
            //}
            //return false;
        }

        public void Undock(GameObject dockedSub)
        {
            using (var log = Log.New())
            {
                ObjectUtil.RequireActive(this, archon.transform);
                if (active)
                {
                    log.Error($"(Un)docking in progress. Cannot undock right now");
                    return;
                }
                if (!dockedSub)
                {
                    log.Error($"Requested sub does not exist");
                    return;
                }
                if (UndockingIsObstructed())
                    return;
                if (!TugFromDocked(dockedSub, false, out var tug, out var dockable, out _))
                    return;
                tug.Bind(this, tug.Fit, TugStatus.UndockingWaitingForBayDoorOpen);
                active = tug;
            }
        }

        public void VerifyIntegrity()
        {
            //the Echelon produces a 'stray' object because it offloads its 3rd person camera, so we can't check this
            //should be harmless, though, as it reintegrates this object after docking has completed
            //foreach (var c in dockedSubRoot.GetChildren())
            //{
            //    if (!Tug.Get(c))
            //    {
            //        //new HierarchyAnalyzer().LogToJson(c, @"C:\temp\stray.json");
            //        throw new InvalidOperationException($"Found stray {c.NiceName()} in docked sub root");
            //    }
            //}

            //var expectDocked = NumDockedVehicles;
            //if (dockedCountWillIncrease)
            //    expectDocked++;
            //if (dockedSubRoot.childCount != expectDocked)
            //{
            //    int actuallyDocked = 0;
            //    foreach (var c in dockedSubRoot.GetChildren())
            //    {
            //        var tug = Tug.Get(c);
            //        if (!tug)
            //            throw new InvalidOperationException($"Found stray child in docked sub root {c.NiceName()}");
            //        else if (tug.Status != TugStatus.UndockedWaitingForTriggerExit)
            //            actuallyDocked++;
            //    }
            //    if (actuallyDocked != expectDocked)
            //        throw new InvalidOperationException($"Wrong child count in docked sub root (is actually {actuallyDocked}, should be {expectDocked})");
            //}
        }


        public void ReleaseActive(Tug tug)
        {
            using (var log = Log.New())
            {

                if (tug.Status == TugStatus.Undocking || tug.Status == TugStatus.UndockedWaitingForTriggerExit)
                    DecNumDockedVehicles(tug);

                if (active == tug)
                {
                    log.Write(nameof(ReleaseActive) + $": {tug}");


                    VerifyIntegrity();
                    active = null;
                }
                else
                    log.Error($"Cannot release active. Requesting tug is {tug}. Expected tug is {active}");
            }
        }

        public bool DoorsAreOpen => progress == 1;
        public bool DoorsAreSufficientlyOpen => progress >= 0.5f;
        public bool DoorsAreClosed => progress == 0;

        public float DoorOpenStatus => progress;

        public int NumUndockableVehicles => Docked.Count();
        public int NumDockedVehicles { get; private set; }

        // Update is called once per frame
        void Update()
        {
            using (var log = Log.NewLazy())
            {
                DockedTugs.Update((id, tug) =>
                {
                    log.Error($"Lost tug [{id}]");
                    NumDockedVehicles--;
                });
                if (isLoading)
                {
                    if (Time.deltaTime == 0)
                    {
                        log.Write($"Loading assumed to continue");
                        return;
                    }
                    isLoading = false;
                    log.Write($"Loading assumed done. Redetecting docked vehicles");
                    RedetectDocked();
                }

                var open = false;
                if (!active)
                {
                    var tugGosActive = dockedSubRoot
                        .GetChildren()
                        .Select(x => x.GetComponent<Tug>())
                        .Where(x => x)
                        .Select(x => x.Fit.GameObject.GetInstanceID())
                        .ToHashSet();


                    var candidate = dockingTrigger.ClosestEnabledNonKinematic(c =>
                    {
                        var go = ObjectUtil.GetGameObject(c);
                        if (tugGosActive.Contains(go.GetInstanceID()))//being tugged (in or out) or docked
                        {
                            //Log.Write($"{go} is already being tugged");
                            return null;
                        }
                        var d = DockingAdapter.ToDockable(go, archon, DockingAdapter.Filter.CurrentlyDockable);
                        if (d == null)
                        {
                            //
                            //Log.Write($"Failed to convert {go} into dockable");
                            return null;
                        }

                        if (go.GetComponent<Tug>())
                        {
                            return null;
                        }

                        var fit = FindBestFit(d);
                        if (fit is null)
                        {
                            OnDockingFailedTooLarge?.Invoke(archon, d);
                            return null;
                        }
                        //Log.Write($"Docking fit {fit.Value.Bounds} {fit.Value.Rotation} {permittedBounds}");

                        if (NumDockedVehicles < maxDockedVehicles)
                            return fit;
                        //Log.Write($"Cannot dock {d}: Docking bay is full");
                        OnDockingFailedFull?.Invoke(archon, d);
                        return null;
                    });
                    open = candidate != null;

                    if (open && DoorsAreSufficientlyOpen)
                    {
                        //move ahead
                        if (candidate is null || candidate.Value.Dockable is null)
                            throw new InvalidOperationException($"Dockable not expected to be invalid here");

                        var tug = Tug.GetOrAdd(candidate.Value.GameObject);
                        //Location.LocalIdentity.ApplyTo(tugObj);
                        //                var tug = tugObj.GetComponent<Tug>();
                        tug.Bind(this, candidate.Value, TugStatus.Docking);
                        active = tug;
                        //log.Debug(nameof(Update) + $": Docking {candidate.Value.Dockable} via {tug}. Incrementing number of docked vehicles");
                        IncNumDockedVehicles(tug);
                    }
                    else if (open)
                    {
                        //Log.Write($"Waiting for doors to open further before docking {candidate}");

                    }
                }
                else
                {
                    ObjectUtil.RequireActive(active, archon.transform);
                    open = active.WantsDoorsOpen;
                }



                float soundPreSeconds = 0.5f;

                float totalSeconds = soundPreSeconds + secondsToOpen;

                var wasClosed = progress <= 0;
                var preProgress = progress;
                if (open)
                    progress = Math.Min(1, progress + Time.deltaTime / totalSeconds);
                else
                    progress = Math.Max(0, progress - Time.deltaTime / totalSeconds);


                //progress = M.Saturate(progress);
                var nowClosed = !open && progress <= 0;

                if (wasClosed && nowClosed)
                {
                    openAnimation.Stop();
                    if (bayDoorSlideSound != null)
                        bayDoorSlideSound.volume = 0;
                    //bayDoorSound.play = false;
                    progress = 0;
                    return;
                }
                if (wasClosed != nowClosed)
                {
                    SetBayVisible(!nowClosed);
                }

                if (wasClosed && !nowClosed)
                {
                    log.Write(nameof(Update) + $": Opening (wasClosed={wasClosed}, nowClosed={nowClosed}, progress={progress}, open={open}) Bay doors opening. Playing lock sound");
                    //progress = -0.5f / secondsToOpen;
                    bayDoorUnlockSound.Play();
                }
                else if (preProgress > soundPreSeconds / totalSeconds && progress <= soundPreSeconds / totalSeconds)
                {
                    log.Write(nameof(Update) + $": Closing (wasClosed={wasClosed}, nowClosed={nowClosed}, preProgress={preProgress}, progress={progress}, open={open}) Bay doors closing. Playing lock sound");

                    bayDoorLockSound.Play();
                }


                if (bayDoorSlideSound != null)
                {
                    if (!bayDoorSlideSound.play)
                        bayDoorSlideSound.volume = 0;
                    else
                        bayDoorSlideSound.volume = (1f - M.Sqr((progress - 0.5f) * 2f)) * initialBayDoorSlideSoundVolume;
                    bayDoorSlideSound.play = true;

                }


                if (!openAnimation.isPlaying)
                    openAnimation.Play();
                float animationProgress = M.Saturate((progress - soundPreSeconds / totalSeconds) / (secondsToOpen / totalSeconds));
                foreach (AnimationState state in openAnimation)
                {
                    state.normalizedTime = M.Saturate(animationProgress);
                }
            }
        }

        public void PrepareForSaving()
        {
            using (var log = Log.New())
            {
                var children = dockedSubRoot.GetChildren().ToList();
                log.Write(nameof(PrepareForSaving) + $" on {children.Count} docked sub candidate(s)");
                for (int i = 0; i < children.Count; i++)
                {
                    var tugCandidate = children[i];
                    try
                    {
                        var tug = Tug.Get(tugCandidate);
                        if (tug && tug.HasGoodFit)
                        {
                            log.Warn($"#{i}/{dockedSubRoot.childCount} {tugCandidate.NiceName()} is valid. Saving");
                            tug.PrepareForSaving();
                        }
                        else
                            log.Warn($"#{i}/{dockedSubRoot.childCount} {tugCandidate.NiceName()} is either not a tug ({tug}) or not well fit. Skipping");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                }
                log.Write(nameof(PrepareForSaving) + $" done");
            }
        }
    }

}