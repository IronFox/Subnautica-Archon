using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

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
    public GameObject tugPrefab;
    

    public int maxDockedVehicles = 2;

    private Bounds permittedBounds;

    public static Action<ArchonControl, IDockable> OnDockingFailedFull { get; set; }
    public static Action<ArchonControl, IDockable> OnDockingFailedTooLarge { get; set; }

    private LogConfig Log { get; } = LogConfig.Default;

    private bool TugFromDocked(GameObject dockedSub, bool destroyIfInvalid, out Tug tug, out IDockable dockable, out UndockingCheckResult undockCheckResult)
        => TugFromGameObject(dockedSub.transform.parent, destroyIfInvalid, out tug, out dockable, out undockCheckResult);
    private bool TugFromGameObject(Transform tugCandidate, bool destroyIfInvalid, out Tug tug, out IDockable dockable, out UndockingCheckResult undockCheckResult)
    {
        if (tugCandidate.childCount != 1)
        {
            Log.LogError($"Tug candidate [{tugCandidate}] has does not have exactly one child (has {tugCandidate.childCount})");
            if (destroyIfInvalid)
            {
                Log.LogError($"Destroying");
                Destroy(tugCandidate);
            }
            tug = null;
            dockable = null;
            undockCheckResult = UndockingCheckResult.NotDocked;
            return false;
        }
        if (tugCandidate.parent != dockedSubRoot)
        {
            Log.LogError($"Tug candidate [{tugCandidate}] resides in wrong parent.");
            if (destroyIfInvalid)
            {
                Log.LogError($"Destroying");
                Destroy(tugCandidate);
            }
            tug = null;
            dockable = null;
            undockCheckResult = UndockingCheckResult.NotDocked;
            return false;
        }
        var sub = tugCandidate.GetChild(0);
        dockable = DockingAdapter.ToDockable(sub.gameObject, archon, DockingAdapter.Filter.All);
        if (dockable == null)
        {
            Log.LogError($"Tug candidate [{tugCandidate}] child [{sub}] failed to convert to dockable");
            if (destroyIfInvalid)
            {
                Log.LogError($"Destroying");
                Destroy(tugCandidate);
            }
            tug = null;
            undockCheckResult = UndockingCheckResult.NotDockable;
            return false;
        }

        tug = tugCandidate.GetComponent<Tug>();
        if (!tug)
        {
            Log.LogError($"Tug candidate {tugCandidate} has no tug. Creating");
            tug = tugCandidate.gameObject.AddComponent<Tug>();
        }
        undockCheckResult = UndockingCheckResult.Ok;
        return true;
    }

    void Awake()
    {
        permittedBounds = dockedBounds.ComputeScaledLocalBounds(includeRenderers:false, includeColliders:true);


        NumDockedVehicles = 0;
        foreach (Transform tugCandidate in dockedSubRoot)
        {
            if (!tugCandidate.name.StartsWith("Tug")
                && tugCandidate.childCount == 0
                && !tugCandidate.GetComponent<Tug>())
            {
                Log.Write($"Tug candidate {tugCandidate} is probably something else. Ignoring");
                continue; //these might be modules. Ignore them
            }
            if (!TugFromGameObject(tugCandidate, true, out var tug, out var dockable, out _))
                continue;

            NumDockedVehicles++;
            tug.Bind(this, tug.Fit, TugStatus.Docked);
        }

    }
    // Start is called before the first frame update
    void Start()
    {
        openAnimation = GetComponent<Animation>();
        SetBayVisible(false);
    }

    private DockingFit? FindBestFit(IDockable d)
    {
        var bounds = d.LocalBounds;

        var fit = new DockingFit(d, Quaternion.identity, -bounds.center, bounds);

        var correction = -bounds.center;
        bounds = bounds.TranslateBy(correction);
        if (!permittedBounds.Contains(fit.Bounds))
        {
            Log.LogError($"Candidate vehicle {d} is too large unrotated. Rotating ({fit.Bounds} exeeds {permittedBounds})");

            bounds = new Bounds(bounds.center, M.V3(bounds.size.x, bounds.size.z, bounds.size.y));
            fit = new DockingFit(d, Quaternion.AngleAxis(90, Vector3.right), -bounds.center, bounds);

            if (!permittedBounds.Contains(fit.Bounds))
            {
                Log.LogError($"Candidate vehicle {d} is still too large to dock ({fit.Bounds} exeeds {permittedBounds})");
                return null;
            }
        }
        return fit;
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
            Log.Write($"Cannot undock right now. Still busy working on {active}");
            return UndockingCheckResult.Busy;
        }
        if (!dockedSub)
        {
            Log.LogError($"Attempted to undock <null> sub");
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
        ObjectUtil.RequireActive(this, archon.transform);
        if (active)
        {
            Log.LogError($"(Un)docking in progress. Cannot undock right now");
            return;
        }
        if (!dockedSub)
        {
            Log.LogError($"Requested sub does not exist");
            return;
        }
        if (UndockingIsObstructed())
            return;
        if (!TugFromDocked(dockedSub, false, out var tug, out var dockable, out _))
            return;
        tug.Bind(this, tug.Fit, TugStatus.WaitingForBayDoorOpen);
        active = tug;
    }


    public void ReleaseActive(Tug tug)
    {
        if (tug.Status == TugStatus.Undocking)
            NumDockedVehicles--;

        if (active == tug)
        {
            Log.Write(nameof(ReleaseActive) + $": {tug}");
            active = null;
        }
        else
            Log.LogError($"Cannot release active. Requesting tug is {tug}. Expected tug is {active}");
    }

    public bool DoorsAreOpen => progress == 1;
    public bool DoorsAreSufficientlyOpen => progress >= 0.5f;
    public bool DoorsAreClosed => progress == 0;

    public float DoorOpenStatus => progress;

    public int NumDockedVehicles { get; private set; }

    // Update is called once per frame
    void Update()
    {
        
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
                var go = ObjectUtil.GetGameObjectOf(c);
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
                var fit = FindBestFit(d);
                if (fit is null)
                {
                    OnDockingFailedTooLarge?.Invoke(archon, d);
                    return null;
                }
                Log.Write($"Docking fit {fit.Value.Bounds} {fit.Value.Rotation} {permittedBounds}");

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

                var tugObj = Instantiate(tugPrefab, dockedSubRoot);
                Location.LocalIdentity.ApplyTo(tugObj);
                var tug = tugObj.GetComponent<Tug>();
                tug.Bind(this, candidate.Value, TugStatus.Docking);
                active = tug;
                NumDockedVehicles++;
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


        var wasClosed = progress == 0;
        if (open)
            progress += Time.deltaTime / secondsToOpen;
        else
            progress -= Time.deltaTime / secondsToOpen;


        progress = M.Saturate(progress);
        var nowClosed = progress == 0;

        if (wasClosed && nowClosed)
        {
            openAnimation.Stop();
            progress = 0;
            return;
        }
        if (wasClosed != nowClosed)
        {
            SetBayVisible(!nowClosed);
        }


        if (!openAnimation.isPlaying)
            openAnimation.Play();
        foreach (AnimationState state in openAnimation)
        {
            state.normalizedTime = progress;
        }
    }


}
