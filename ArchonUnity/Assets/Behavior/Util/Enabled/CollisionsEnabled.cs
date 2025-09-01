using UnityEngine;

namespace Assets.Behavior.Util.Enabled
{
    /// <summary>
    /// IEnabled implementation for Rigidbody.detectCollisions.
    /// </summary>
    internal class CollisionsEnabled : IEnabled
    {
        private readonly Rigidbody c;

        public CollisionsEnabled(Rigidbody c)
        {
            this.c = c;
        }

        public Object Target => c;

        public bool IsEnabled => c.detectCollisions;

        public bool LogChange => true;

        public string PropertyName => "detectCollisions";

        public bool SetEnabled(bool enabled)
        {
            if (c.detectCollisions == enabled)
                return false;
            c.detectCollisions = enabled;
            return true;
        }
        public bool Equals(IEnabled other) => other is CollisionsEnabled r && r.c == c;

    }
}