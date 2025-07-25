using UnityEngine;

namespace Assets.Behavior.Components.Motion
{
    public static class LeanIntensityCalculator
    {
        public static float CalculateLeanIntensity(float depth, float speed, float maxSpeed, float minLeanIntensity = 0.02f, float maxLeanIntensity = 1.0f)
        {
            if (maxSpeed <= 0 || depth <= 0)
            {
                return 0f; // No speed means no lean and no depth means no lean effect
            }
            // Normalize speed to a value between 0 and 1
            float normalizedSpeed = M.Sqr(Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed));
            // Interpolate between min and max lean intensity based on normalized speed
            return Mathf.Lerp(minLeanIntensity, maxLeanIntensity, normalizedSpeed)
                // Adjust based on depth
                * M.Smoothstep(5, 15, depth);
        }
    }
}
