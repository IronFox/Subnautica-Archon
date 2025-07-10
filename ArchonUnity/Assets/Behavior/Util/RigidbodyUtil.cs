using UnityEngine;

public static class RigidbodyUtil
{
    public static void SetKinematic(this Rigidbody rb)
    {
        LogConfig.Default.Write($"Setting [{rb.NiceName()}].isKinematic := true");
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        //rb.interpolation = RigidbodyInterpolation.None;
    }

    public static void UnsetKinematic(this Rigidbody rb)
    {
        LogConfig.Default.Write($"Setting [{rb.NiceName()}].isKinematic := false");
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
                LogConfig.Default.LogWarning("Re-enabling kinematic state");
                rb.SetKinematic();
            }
            else
            {
                LogConfig.Default.LogWarning("Re-disabling kinematic state");
                rb.UnsetKinematic();
            }
        }
    }
}