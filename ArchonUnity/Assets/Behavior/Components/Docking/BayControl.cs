using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BayControl : MonoBehaviour
{
    public float secondsToOpen = 3;
    //public bool open;
    private float progress = 0;
    private new Animation animation;

    public TriggerTracker dockingTrigger;

    private Tug active;

    public float dockingMetersPerSecond = 10;

    public Transform insides;
    public ArchonControl archon;
    public Transform dockedSubRoot;
    public Transform dockedBounds;
    public Transform dockingColliders;



    private LogConfig Log { get; } = LogConfig.Default;

    // Start is called before the first frame update
    void Start()
    {
        animation = GetComponent<Animation>();
        SetBayVisible(false);
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
        if (active)
            return UndockingCheckResult.CannotBusy;
        if (dockedSub && dockedSub.transform.parent == dockedSubRoot)
        {
            var dockable = DockingAdapter.ToDockable(dockedSub, archon);
            if (dockable != null)
                return UndockingCheckResult.Possible;
            return UndockingCheckResult.NotDockable;
        }
        else
            return UndockingCheckResult.CannotNotDocked;
    }


    public void Undock(GameObject dockedSub)
    {
        if (active)
        {
            Log.LogError($"(Un)docking in progress. Cannot undock right now");
            return;
        }
        if (dockedSub && dockedSub.transform.parent == dockedSubRoot)
        {
            var dockable = DockingAdapter.ToDockable(dockedSub, archon);
            if (dockable != null)
            {
                var tug = Tug.GetOf(dockedSub);
                tug.Bind(this, dockable, TugStatus.WaitingForBayDoorOpen);
                active = tug;
            }
            else
            {
                Log.LogError($"Docked sub {dockedSub} could not be converted to dockable");
            }
        }
        else
            Log.LogError($"Docked sub {dockedSub} does not exist or is not in {dockedSubRoot}");
    }

    private static GameObject GameObjectOf(Collider collider)
    {
        if (collider.attachedRigidbody)
            return collider.attachedRigidbody.gameObject;
        return collider.gameObject;
    }

    public void SignalSavegameLoadingDone()
    {
        foreach (Transform child in dockedSubRoot)
        {
            var dockable = DockingAdapter.ToDockable(child.gameObject, archon);
            if (dockable == null)
            {
                Log.LogError($"Contained transform {child} does not resolve to dockable. Deleting");
                Destroy(child.gameObject);
                continue;
            }
            var tug = Tug.GetOf(child);
            
            tug.Bind(this, dockable, child.localPosition == dockedBounds.transform.localPosition ? TugStatus.Docked : TugStatus.Docking);
        }
    }

    public void ReleaseActive(Tug tug)
    {
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

    // Update is called once per frame
    void Update()
    {
        var open = false;
        if (!active)
        {
            var candidate = dockingTrigger.ClosestEnabledNonKinematic(c =>
            {
                var go = GameObjectOf(c);
                if (go.GetComponent<Tug>()) //being tugged (in or out) or docked
                    return null;
                return DockingAdapter.ToDockable(go, archon);
            });
            open = candidate != null;

            if (open && DoorsAreSufficientlyOpen)
            {
                //move ahead

                var tug = candidate.GameObject.AddComponent<Tug>();
                tug.Bind(this, candidate, TugStatus.Docking);
                active = tug;
            }

        }
        else
        {
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
            animation.Stop();
            return;
        }
        if (wasClosed != nowClosed)
        {
            SetBayVisible(!nowClosed);
        }


        if (!animation.isPlaying)
            animation.Play();
        foreach (AnimationState state in animation)
        {
            state.normalizedTime = progress;
        }

        if (progress == 0 || progress == 1)
        {
            animation.Stop();
        }
    }


}
