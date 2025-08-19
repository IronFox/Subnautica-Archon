using Assets.Behavior.Adapters;
using Behavior.Util;
using Behavior.Util.Log;
using UnityEngine;

public static class RigidbodyUtil
{
    public static void SetKinematic(this Rigidbody rb)
    {
        if (rb && rb.isKinematic)
            return;
        using (var log = new LogContext(nameof(SetKinematic)))
        {
            if (!rb)
            {
                log.Error("Rigidbody is null, cannot set kinematic state.");
                return;
            }

            log.Write($"Setting [{rb.NiceName()}].isKinematic := true");
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            //rb.interpolation = RigidbodyInterpolation.None;
        }
    }

    public static void UnsetKinematic(this Rigidbody rb)
    {
        if (rb && !rb.isKinematic)
            return;
        using (var log = new LogContext(nameof(UnsetKinematic)))
        {
            if (!rb)
            {
                log.Error("Rigidbody is null, cannot unset kinematic state.");
                return;
            }

            log.Write($"Setting [{rb.NiceName()}].isKinematic := false");
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Extrapolate;
        }
    }

    public static void CheckIsKinematic(this Rigidbody rb, bool shouldBeKinematic)
    {

        if (rb.isKinematic != shouldBeKinematic)
        {
            using (var log = new LogContext(nameof(CheckIsKinematic)))
            {

                if (shouldBeKinematic)
                {
                    log.Warn("Re-enabling kinematic state");
                    rb.SetKinematic();
                }
                else
                {
                    log.Warn("Re-disabling kinematic state");
                    rb.UnsetKinematic();
                }
            }
        }
    }
}