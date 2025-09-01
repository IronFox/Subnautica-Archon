using Assets.Behavior.Adapters;
using UnityEngine;

namespace Assets.Behavior.Util
{
    /// <summary>
    /// Utility methods for working with Rigidbodies.
    /// </summary>
    public static class RigidbodyUtil
    {
        public static bool SetKinematic(this Rigidbody rb)
        {
            if (rb && rb.isKinematic)
                return false;
            using (var log = Log.NewLazy())
            {
                if (!rb)
                {
                    log.Error("Rigidbody is null, cannot set kinematic state.");
                    return false;
                }

                log.Debug($"Setting [{rb.NiceName()}].isKinematic := true");
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                //rb.interpolation = RigidbodyInterpolation.None;
                return true;
            }
        }

        public static bool UnsetKinematic(this Rigidbody rb)
        {
            if (rb && !rb.isKinematic)
                return false;
            using (var log = Log.NewLazy())
            {
                if (!rb)
                {
                    log.Error("Rigidbody is null, cannot unset kinematic state.");
                    return false;
                }

                log.Debug($"Setting [{rb.NiceName()}].isKinematic := false");
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Extrapolate;
                return true;
            }
        }

        public static void CheckIsKinematic(this Rigidbody rb, bool shouldBeKinematic)
        {

            if (rb.isKinematic != shouldBeKinematic)
            {
                using (var log = Log.New())
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
}