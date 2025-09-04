using UnityEngine;

namespace Assets.Behavior.Util.Lighting
{
    public static class InteriorLightBuilder
    {
        public static InteriorLightColor Build(WeightedLightState avg)
        {

            Color lightColor = M.Gray(avg.InteriorLightScale);
            Color stripColor = lightColor;

            var chargeColor = new Color(0.6f, 0.6f, 1.2f, 1f);
            var chargePulse = 0.5f + (0.5f * Mathf.Sin(Time.time * 3));

            stripColor = Color.LerpUnclamped(
                stripColor,
                chargeColor,
                chargePulse * avg.IsCharging);


            var alertRedBrightness = avg.InteriorLightScale * (0.3f + (0.3f * Mathf.Sin(Time.time * 3f)));

            stripColor = Color.LerpUnclamped(
                stripColor,
                new Color(
                    alertRedBrightness,
                    0,
                    0),
                avg.IsDead);
            lightColor = Color.LerpUnclamped(
                lightColor,
                new Color(
                    alertRedBrightness,
                    0,
                    0),
                avg.IsDead);
            stripColor = Color.LerpUnclamped(
                stripColor,
                Color.black,
                1f - avg.InteriorLightsEnabled);
            lightColor = Color.LerpUnclamped(
                lightColor,
                Color.black,
                1f - avg.InteriorLightsEnabled);


            return new InteriorLightColor(
                stripColor: stripColor,
                lightColor: lightColor);
        }
    }
}
