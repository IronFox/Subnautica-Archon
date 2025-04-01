using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EvacuateIntruders : MonoBehaviour
{
    public Transform exteriorColliderRoot;
    private readonly List<Sphere> localSpheres = new List<Sphere>();
    private Collider[] buffer = new Collider[256];
    private readonly HashSet<GameObject> shove = new HashSet<GameObject> ();

    private readonly MultiFrameJob myJob;

    private LogConfig log = new LogConfig("Intruders",true);

    public EvacuateIntruders()
    {
        myJob = new MultiFrameJob(
            _ =>
            {
                shove.Clear();
                return new AdvanceJob(localSpheres);
            },
            sphere_ =>
            {
                var sphere = (Sphere)sphere_;
                var hits = Physics.OverlapSphereNonAlloc(transform.TransformPoint(sphere.Center), sphere.Radius, buffer);
                shove.AddRange(buffer
                    .Take(hits)
                    .Select(hit => hit.attachedRigidbody
                        ? hit.attachedRigidbody.gameObject
                        : hit.gameObject
                        ));
                if (hits >= buffer.Length)
                {
                    buffer = new Collider[buffer.Length * 2];
                    log.LogWarning($"Resized buffer to {buffer.Length}");
                }
                return new AdvanceJob(shove);
            },
            shove_ =>
            {
                try
                {
                    var candidate = (GameObject)shove_;
                    if (!candidate)
                        return default;

                    if (!candidate.transform.IsChildOf(transform.parent) && EvacuationAdapter.ShouldEvacuate(candidate))
                    {
                        List<Collider> disabled = new List<Collider>();
                        var colliders = exteriorColliderRoot.GetComponentsInChildren<Collider>().ToDictionary(x => x.GetInstanceID());
                        foreach (var exteriorCollider in colliders.Values)
                        {
                            var isEnabled = exteriorCollider.enabled;
                            if (!isEnabled)
                            {
                                disabled.Add(exteriorCollider);
                                exteriorCollider.enabled = true;
                            }
                        }

                        try
                        {
                            const float outerRadius = 100;

                            var what = candidate.transform.position;
                            var from = transform.position;

                            var fromToWhat = what - from;
                            Vector3 p;
                            float d2 = fromToWhat.sqrMagnitude;
                            if (d2 > M.Sqr(outerRadius))
                                return default;
                            if (d2 == 0)    //can't push
                            {
                                log.LogWarning($"Candidate {candidate} is at distance 0");
                                return default;
                            }

                            var distanceToCandidiate = Mathf.Sqrt(d2);

                            p = from + fromToWhat * outerRadius / distanceToCandidiate;

                            var inwards = (transform.position - p);
                            var hits = Physics.RaycastAll(new Ray(p, inwards / outerRadius), outerRadius);
                            var exteriorHits = hits.Where(x => colliders.ContainsKey( x.collider.GetInstanceID()));
                            if (exteriorHits.Any())
                            {
                                var hitDistance = hits.Select(x => x.distance).Max();
                                var exteriorRadius = outerRadius - hitDistance;

                                var r = 1f;
                                foreach (var collider in candidate.GetComponentsInChildren<Collider>())
                                {
                                    r = M.Max(r, (M.Abs(candidate.transform.InverseTransformPoint(collider.transform.position)) + collider.bounds.extents).sqrMagnitude);
                                }
                                r = Mathf.Sqrt(r);

                                if (exteriorRadius > distanceToCandidiate - r)
                                {
                                    var targetPosition = from + fromToWhat * (outerRadius + r * 1.2f) / distanceToCandidiate;
                                    log.LogWarning($"Evacuating {candidate} to {targetPosition} at extR={exteriorRadius}, dist={distanceToCandidiate}, r={r} ");
                                    candidate.transform.position = targetPosition;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                        finally
                        {
                            foreach (var c in disabled)
                                c.enabled = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                return default;
            }

        );
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
        log.Write($"Identified {localSpheres.Count} local spheres");
    }




    // Update is called once per frame
    void Update()
    {
        myJob.Next();
        
        //bool wasEnabled = collider.enabled;

        //Physics.OverlapBoxNonAlloc(



    }
}
