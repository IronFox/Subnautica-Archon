using System;

namespace Assets.Behavior.Util
{
    public readonly struct InteriorLightState : IEquatable<InteriorLightState>
    {
        public bool Enabled { get; }
        public bool Alert { get; }
        public float Intensity { get; }

        public InteriorLightState(bool enabled, float intensity, bool alert)
        {
            Enabled = enabled;
            Intensity = intensity;
            Alert = alert;
        }

        public bool Equals(InteriorLightState other)
            => Enabled == other.Enabled
            && Intensity.Equals(other.Intensity)
            && Alert == other.Alert;

        public override bool Equals(object obj)
        {
            if (obj is InteriorLightState other)
                return Equals(other);
            return false;
        }
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Enabled.GetHashCode();
            hash = hash * 23 + Intensity.GetHashCode();
            hash = hash * 23 + Alert.GetHashCode();
            return hash;
        }
        public override string ToString()
        {
            return $"InteriorLightState(Enabled: {Enabled}, Intensity: {Intensity}, Alert:{Alert})";
        }
        public static bool operator ==(InteriorLightState left, InteriorLightState right)
            => left.Equals(right);
        public static bool operator !=(InteriorLightState left, InteriorLightState right)
            => !left.Equals(right);
    }
}
