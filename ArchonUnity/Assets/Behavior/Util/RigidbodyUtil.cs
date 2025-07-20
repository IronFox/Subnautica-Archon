using Assets.Behavior.Adapters;
using UnityEngine;

public static class RigidbodyUtil
{
    public static void SetKinematic(this Rigidbody rb)
    {
        if (!rb.isKinematic)
            Log.Default.Write($"Setting [{rb.NiceName()}].isKinematic := true");
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        //rb.interpolation = RigidbodyInterpolation.None;
    }

    public static void UnsetKinematic(this Rigidbody rb)
    {
        if (rb.isKinematic)
            Log.Default.Write($"Setting [{rb.NiceName()}].isKinematic := false");
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;
    }

    public static void CheckIsKinematic(this Rigidbody rb, bool shouldBeKinematic)
    {

        if (rb.isKinematic != shouldBeKinematic)
        {
            if (shouldBeKinematic)
            {
                Log.Default.LogWarning("Re-enabling kinematic state");
                rb.SetKinematic();
            }
            else
            {
                Log.Default.LogWarning("Re-disabling kinematic state");
                rb.UnsetKinematic();
            }
        }
    }
}