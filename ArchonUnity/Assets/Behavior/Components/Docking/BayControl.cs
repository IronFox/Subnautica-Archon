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

    private Tug docking;

    public float dockingMetersPerSecond = 10;

    public Transform insides;
    public ArchonControl subRoot;
    public Transform loaded;
    public Transform dockedBounds;
    private bool dockingDoneCloseDoors;
    private Action callWhenDoorsClosed;

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
    }

    private static GameObject GameObjectOf(Collider collider)
    {
        if (collider.attachedRigidbody)
            return collider.attachedRigidbody.gameObject;
        return collider.gameObject;
    }

    public void SignalLoadDone()
    {
        foreach (Transform child in loaded)
        {
            var dockable = DockingAdapter.ToDockable(child.gameObject, subRoot);
            if (dockable == null)
            {
                Log.LogError($"Contained transform {child} does not resolve to dockable. Deleting");
                Destroy(child.gameObject);
                continue;
            }
            var tug = child.gameObject.GetComponent<Tug>();
            if (!tug)
                tug = child.gameObject.AddComponent<Tug>();
            
            tug.Bind(this, dockable, child.localPosition == dockedBounds.transform.localPosition ? TugStatus.Docked : TugStatus.Docking);
        }
    }

    public void SignalDockingDone(Tug tug, Action callWhenDoorsClosed)
    {
        if (docking == tug)
        {
            dockingDoneCloseDoors = true;
            this.callWhenDoorsClosed = callWhenDoorsClosed;
            //docking = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        var open = false;
        if (!docking)
        {
            var candidate = dockingTrigger.ClosestEnabledNonKinematic(c =>
            {
                var go = GameObjectOf(c);
                if (go.GetComponent<Tug>()) //being tugged (in or out) or docked
                    return null;
                return DockingAdapter.ToDockable(go, subRoot);
            });
            open = candidate != null;

            if (open && progress >= 0.35f)
            {
                //move ahead

                var tug = candidate.GameObject.AddComponent<Tug>();
                tug.Bind(this, candidate, TugStatus.Docking);
                docking = tug;
            }

        }
        else
        {
            open = !dockingDoneCloseDoors;
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
            if (nowClosed)
            {
                if (dockingDoneCloseDoors)
                {
                    dockingDoneCloseDoors = false;
                    if (callWhenDoorsClosed != null)
                        callWhenDoorsClosed();
                    docking = null;
                }

            }
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
