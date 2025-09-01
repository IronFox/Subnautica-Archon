using UnityEngine;

namespace Assets.Behavior.Util.Enabled
{

    internal class BehaviourEnabled : IEnabled
    {
        private readonly Behaviour x;

        public BehaviourEnabled(Behaviour x)
        {
            this.x = x;
        }

        public Object Target => x;

        public bool IsEnabled => x.enabled;

        public bool LogChange => true;

        public string PropertyName => "enabled";

        public bool SetEnabled(bool enabled)
        {
            if (x.enabled == enabled)
                return false;
            x.enabled = enabled;
            return true;
        }
        public bool Equals(IEnabled other) => other is BehaviourEnabled r && r.x == x;

    }
}