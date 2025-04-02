using System;
using UnityEngine;

public static class RigidbodyUtil
{
    public static void SetKinematic(this Rigidbody rb)
    {
        LogConfig.Default.Write($"Setting [{rb}].isKinematic := true");
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        //rb.interpolation = RigidbodyInterpolation.None;
    }

    public static void UnsetKinematic(this Rigidbody rb)
    {
        LogConfig.Default.Write($"Setting [{rb}].isKinematic := false");
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;
    }
}