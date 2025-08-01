using UnityEngine;

namespace Assets.Behavior.Util
{
    public readonly struct InteriorLightColor
    {
        public Color StripColor { get; }
        public Color LightColor { get; }

        public float Recorded { get; }

        public InteriorLightColor(Color stripColor, Color lightColor)
        {
            StripColor = stripColor;
            LightColor = lightColor;
            Recorded = Time.time;
        }

        public static InteriorLightColor Lerp(InteriorLightColor interpolatingFrom, InteriorLightColor newColor, float v)
        {
            if (v <= 0)
                return interpolatingFrom;
            if (v >= 1)
                return newColor;
            var stripColor = Color.Lerp(interpolatingFrom.StripColor, newColor.StripColor, v);
            var lightColor = Color.Lerp(interpolatingFrom.LightColor, newColor.LightColor, v);
            return new InteriorLightColor(stripColor, lightColor);
        }
    }
}
