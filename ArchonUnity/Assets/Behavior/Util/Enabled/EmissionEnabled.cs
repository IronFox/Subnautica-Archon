using UnityEngine;

namespace Assets.Behavior.Util.Enabled
{
    /// <summary>
    /// Enables or disables the emission module of a ParticleSystem.
    /// </summary>
    internal class EmissionEnabled : IEnabled
    {
        private readonly ParticleSystem x;

        public EmissionEnabled(ParticleSystem x)
        {
            this.x = x;
        }

        public Object Target => x;

        public bool IsEnabled => x.emission.enabled;

        public bool LogChange => true;

        public string PropertyName => "enabled";

        public bool SetEnabled(bool enabled)
        {
            var em = x.emission;
            if (em.enabled == enabled)
                return false;
            em.enabled = enabled;
            return true;
        }
        public bool Equals(IEnabled other) => other is EmissionEnabled r && r.x == x;
    }
}