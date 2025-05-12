using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EvacuateIntruders : MonoBehaviour
{
    public Transform exteriorColliderRoot;
    public MeshCollider interiorCollider;
    private readonly List<Sphere> localSpheres = new List<Sphere>();
    private Collider[] buffer = new Collider[256];

    private LogConfig log = new LogConfig("Intruders", true);
    private Coroutine myRoutine;


    private IEnumerator Run()
    {
        Dictionary<int, TestCandidate> candidates = new Dictionary<int, TestCandidate>();
        var outerRadius = 500;
        var allHits = new List<Hit>();
        while (true)
        {
            if (!this.enabled)
            {
                yield return null;
                continue;
            }
            candidates.Clear();
            foreach (var sphere in localSpheres)
            {
                yield return null;
                var hits = Physics.OverlapSphereNonAlloc(transform.TransformPoint(sphere.Center), sphere.Radius, buffer);
                var rs = buffer
                    .Take(hits)
                    .Select(hit => hit.attachedRigidbody
                        ? hit.attachedRigidbody.gameObject
                        : hit.gameObject
                        );

                foreach (var hit in rs)
                {
                    if (!candidates.TryGetValue(hit.GetInstanceID(), out var candidate))
                    {
                        candidate = new TestCandidate(hit);
                        candidates.Add(hit.GetInstanceID(), candidate);
                    }
                    candidate.Spheres.Add(sphere);
                }

                if (hits >= buffer.Length)
                {
                    buffer = new Collider[buffer.Length * 2];
                    log.LogWarning($"Resized buffer to {buffer.Length}");
                }
            }


            foreach (var candidate in candidates.Values)
            {
                yield return null;
                try
                {
                    if (!candidate.GameObject)
                        continue;
                    if (EvacuationAdapter.ShouldKeep(candidate.GameObject))
                    {
                        try
                        {
                            allHits.Clear();
                            var what = candidate.GameObject.transform.position;
                            foreach (var sphere in candidate.Spheres)
                            {
                                var localSphere = sphere;
                                var me = transform.TransformPoint(localSphere.Center);
                                var meToWhat = what - me;
                                float d2 = meToWhat.sqrMagnitude;
                                if (d2 == 0)    //can't pull
                                {
                                    log.LogWarning($"Pull candidate {candidate} is at distance 0");
                                    continue;
                                }


                                var distanceToCandidiate = Mathf.Sqrt(d2);
                                var dir = meToWhat / distanceToCandidiate;
                                var hits = Physics.RaycastAll(new Ray(me, dir), outerRadius, ArchonControl.OuterShellLayer);
                                var interiorHits = hits.Where(x => x.collider == interiorCollider);
                                allHits.AddRange(interiorHits.Select(x =>
                                new Hit(
                                    x,
                                    sphere,
                                    me,
                                    dir,
                                    distanceToCandidiate,
                                    candidate)));
                            }

                            Vector3 p;
                            int needsRelocation = 0;
                            foreach (var hit in allHits)
                            {
                                var hitDistance = hit.DistanceToHull;
                                if (hit.DistanceToHull < hit.DistanceToCandidate)
                                {
                                    needsRelocation++;
                                }
                            }
                            if (allHits.Any() && needsRelocation == allHits.Count)
                            {
                                var least = allHits.Where(x => x.DistanceToHull < x.DistanceToCandidate).Least(x => x.DistanceToCandidate);
                                var targetPosition = least.Origin + least.Direction * (least.DistanceToCandidate - 1);
                                log.LogWarning($"Re-integrating {candidate.GameObject.NiceName()} to {targetPosition} at intR={least.DistanceToHull}, dist={least.DistanceToCandidate} ");
                                candidate.GameObject.transform.position = targetPosition;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                    else if (!candidate.GameObject.transform.IsChildOf(transform.parent)
                        && EvacuationAdapter.ShouldEvacuate(candidate.GameObject))
                    {
                        using (var colliders = ReEnableColliders())
                        {
                            try
                            {
                                var what = candidate.GameObject.transform.position;
                                allHits.Clear();
                                foreach (var sphere in candidate.Spheres)
                                {
                                    var me = transform.TransformPoint(sphere.Center);
                                    var meToWhat = what - me;
                                    Vector3 p;
                                    float candidateDistance2 = meToWhat.sqrMagnitude;
                                    if (candidateDistance2 > M.Sqr(outerRadius))
                                        continue;
                                    if (candidateDistance2 == 0)    //can't push
                                    {
                                        log.LogWarning($"Candidate {candidate} is at distance 0");
                                        continue;
                                    }
                                    var distanceToCandidiate = Mathf.Sqrt(candidateDistance2);

                                    var meToWhatDir = meToWhat / distanceToCandidiate;

                                    p = me + meToWhatDir * outerRadius;

                                    var inwards = (me - p).normalized;
                                    var hits = Physics.RaycastAll(new Ray(p, inwards), outerRadius);
                                    var exteriorHits = hits.Where(x => colliders.Contains(x.collider));
                                    allHits.AddRange(exteriorHits.Select(x =>
                                    new Hit(
                                        x,
                                        sphere,
                                        me,
                                        inwards,
                                        distanceToCandidiate,
                                        candidate)));
                                }

                                if (allHits.Any())
                                {
                                    var hit = allHits.Least(x => x.DistanceToHull);
                                    var exteriorRadius = outerRadius - hit.DistanceToHull;

                                    var r = 1f;
                                    foreach (var collider in candidate.GameObject.GetComponentsInChildren<Collider>())
                                    {
                                        r = M.Max(r, (M.Abs(candidate.GameObject.transform.InverseTransformPoint(collider.transform.position)) + collider.bounds.extents).sqrMagnitude);
                                    }
                                    r = Mathf.Sqrt(r);

                                    if (exteriorRadius > hit.DistanceToCandidate - r)
                                    {
                                        var targetPosition = hit.Origin + -hit.Direction * (outerRadius + r * 1.2f);
                                        log.LogWarning($"Evacuating {candidate} to {targetPosition} at extR={exteriorRadius}, dist={hit.DistanceToCandidate}, r={r} ");
                                        candidate.GameObject.transform.position = targetPosition;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }


    ColliderChange ReEnableColliders()
    {
        List<Collider> disabled = new List<Collider>();
        var colliders = exteriorColliderRoot.GetComponentsInChildren<Collider>().ToDictionary(x => x.GetInstanceID());
        return new ColliderChange(colliders);
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform child in transform)
        {
            var sphere = child.GetComponent<SphereCollider>();
            if (!sphere)
                continue;
            localSpheres.Add(new Sphere(sphere.transform.localPosition, sphere.radius * sphere.transform.localScale.x));
        }
        log.Write($"Identified {localSpheres.Count} local spheres. Starting coroutine");
        myRoutine = StartCoroutine(Run());
    }

    void OnDestroy()
    {
        if (myRoutine != null)
            StopCoroutine(myRoutine);
    }



    // Update is called once per frame
    void Update()
    {

        //bool wasEnabled = collider.enabled;

        //Physics.OverlapBoxNonAlloc(



    }
}

internal class ColliderChange : IDisposable
{

    public ColliderChange(Dictionary<int, Collider> colliders)
    {
        Colliders = colliders;
        foreach (var exteriorCollider in colliders.Values)
        {
            var isEnabled = exteriorCollider.enabled;
            if (!isEnabled)
            {
                disabled.Add(exteriorCollider);
                exteriorCollider.enabled = true;
            }
        }
    }

    private readonly List<Collider> disabled = new List<Collider>();
    public Dictionary<int, Collider> Colliders { get; }

    public void Dispose()
    {
        foreach (var exteriorCollider in disabled)
        {
            exteriorCollider.enabled = false;
        }

    }

    public bool Contains(Collider c) => Colliders.ContainsKey(c.GetInstanceID());
}

internal readonly struct Hit
{
    public RaycastHit RaycastHit { get; }
    public Sphere Sphere { get; }
    public Vector3 Origin { get; }
    public Vector3 Direction { get; }
    public float DistanceToHull => RaycastHit.distance;
    public float DistanceToCandidate { get; }
    public TestCandidate Candidate { get; }


    public Hit(RaycastHit raycastHit, Sphere sphere, Vector3 origin, Vector3 direction, float distanceToCandidiate, TestCandidate candidate)
    {
        RaycastHit = raycastHit;
        Sphere = sphere;
        Origin = origin;
        Direction = direction;
        DistanceToCandidate = distanceToCandidiate;
        Candidate = candidate;
    }
}

internal class TestCandidate
{
    public GameObject GameObject { get; }
    public List<Sphere> Spheres { get; } = new List<Sphere>();
    public TestCandidate(GameObject gameObject)
    {
        GameObject = gameObject;
    }
}