using UnityEngine;

namespace Assets.Behavior.Util.Enabled
{
    /// <summary>
    /// IEnabled implementation for Collider components.
    /// </summary>
    internal class ColliderEnabled : IEnabled
    {
        private readonly Collider x;

        public ColliderEnabled(Collider x)
        {
            this.x = x;
        }

        public Object Target => x;

        public bool IsEnabled => x.enabled;

        public bool LogChange => false;

        public string PropertyName => "enabled";

        public bool SetEnabled(bool enabled)
        {
            if (x.enabled == enabled)
                return false;
            x.enabled = enabled;
            return true;
        }

        public bool Equals(IEnabled other) => other is ColliderEnabled r && r.x == x;

    }
}