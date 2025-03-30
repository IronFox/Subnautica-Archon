using System;
using UnityEngine;

public static class RigidbodyUtil
{
    public static void SetKinematic(this Rigidbody rb)
    {
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.None;
    }

    public static void UnsetKinematic(this Rigidbody rb)
    {
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;
    }
}