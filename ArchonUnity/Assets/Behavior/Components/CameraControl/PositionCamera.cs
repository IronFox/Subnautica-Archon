﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PositionCamera : MonoBehaviour
{
    // Start is called before the first frame update

    public BoxCollider referenceBoundingBox;
    public Rigidbody subRoot;
    private float distanceToTarget;
    private Transform target;
    private float minDistanceToTarget;
    private float maxDistanceToTarget;
    public bool positionBelowTarget;
    public Collider shipCollider;

    
    //private float firstPersonRadius = 3.15f;
    public bool isFirstPerson = false;
    private bool wasFirstPerson;

    public float defaultThirdPersonDistance = 10.12f;

    private float verticalOffset;

    private float h = 0;
    public float zoomAxis;

    //private TargetScanner scanner;

    public float DistanceToTarget => distanceToTarget;

    void Start()
    {
        //scanner= GetComponentInChildren<TargetScanner>();
        target = subRoot.transform;
        distanceToTarget = Vector3.Distance(referenceBoundingBox.transform.position, transform.transform.position);


        minDistanceToTarget = (referenceBoundingBox.transform.localPosition.z
                                - referenceBoundingBox.transform.localScale.z
                                    * referenceBoundingBox.size.z) * -0.5f;
        maxDistanceToTarget = minDistanceToTarget * 5;
        ConsoleControl.Write($"Valid 3rd person camera distance range is [{minDistanceToTarget},{maxDistanceToTarget}]");
        distanceToTarget = Mathf.Clamp( distanceToTarget, minDistanceToTarget, maxDistanceToTarget );
        ConsoleControl.Write($"3rd camera distance set to {distanceToTarget}");
        verticalOffset = 
            referenceBoundingBox.size.y * referenceBoundingBox.transform.localScale.y * 1.1f;
    }

    private string loggedCollider;

    void LateUpdate()
    {
        if (isFirstPerson)
        {
            wasFirstPerson = true;


            transform.position = target.position + target.forward * (referenceBoundingBox.size.z / 2 + 5);


            //var local = subRoot.transform.InverseTransformDirection(transform.forward);

            //float forwardRadius = (M.Abs(local.x) * referenceBoundingBox.size.x + M.Abs(local.y) * referenceBoundingBox.size.y + M.Abs(local.z) * referenceBoundingBox.size.z*1.1f) * 0.5f;
            //if (Physics.Raycast(new Ray(target.position, transform.forward), out var hit, forwardRadius, ~(1 << 30)))
            //{
            //    forwardRadius = hit.distance - 1f;
            //}

            //transform.position = target.position + transform.forward * forwardRadius;
            if (zoomAxis > 0)
            {
                isFirstPerson = false;
            }
        }
        else
        {
            if (wasFirstPerson)
            {
                transform.position = target.position - transform.forward * minDistanceToTarget;
                wasFirstPerson = false;
                distanceToTarget = minDistanceToTarget;
            }

            distanceToTarget *= Mathf.Pow(1.5f, zoomAxis);
            if (distanceToTarget < minDistanceToTarget)
            {
                distanceToTarget = minDistanceToTarget;
                isFirstPerson = true;
                return;
            }
            distanceToTarget = Mathf.Clamp(distanceToTarget, minDistanceToTarget, maxDistanceToTarget);

            //scanner.minDistance = distanceToTarget;


            var wantH = positionBelowTarget ? -verticalOffset : verticalOffset;

            h += (wantH - h) * 2f * Mathf.Min(Time.deltaTime, 1f);

            var lookAtTarget = target.position + /*referenceBoundingBox.transform.up*/Vector3.up * h;

            var wantPosition = lookAtTarget - transform.forward * distanceToTarget;
            Vector3 targetPosition;


            var dir = -transform.forward;
            //var hits = Physics.RaycastAll(lookAtTarget, dir, distanceToTarget);

            var dir2 = wantPosition - target.position;
            var dist2 = dir2.magnitude;
            dir2 /= dist2;


            var hits2 = Physics.RaycastAll(target.position, dir2, dist2);


            float closestHit2 = Mathf.Infinity;
            Transform closest2 = null;
            foreach (RaycastHit hit in hits2)
            {
                if (hit.transform.IsChildOf(target)
                    || hit.transform.IsChildOf(transform)
                    || Physics.GetIgnoreCollision(hit.collider, shipCollider)
                    || !hit.collider.enabled
                    || hit.collider.isTrigger)
                    continue;
                if (hit.distance < closestHit2)
                {
                    closest2 = hit.transform;
                    closestHit2 = hit.distance;
                    if (loggedCollider != hit.transform.name)
                    {
                        loggedCollider = hit.transform.name;
                        //ConsoleControl.Write("Camera collision with " + hit.transform.name);
                        //HierarchyAnalyzer analyzer = new HierarchyAnalyzer();
                        //analyzer.LogToJson(hit.transform, $@"C:\temp\logs\hit{DateTime.Now:yyyy-MM-dd HH_mm_ss}.json");
                    }



                }
            }


            if (closest2 != null)
                targetPosition = target.position + dir2 * Mathf.Max(3f, closestHit2 - 0.5f);
            else
                targetPosition = wantPosition;

            transform.position = targetPosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
