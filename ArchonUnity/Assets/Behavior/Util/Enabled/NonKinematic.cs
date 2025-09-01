using UnityEngine;

namespace Assets.Behavior.Util.Enabled
{
    /// <summary>
    /// An IEnabled that is true when a Rigidbody is non-kinematic.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("NonKinematic: {RB.NiceName()}")]
    internal class NonKinematic : IEnabled
    {
        public NonKinematic(Rigidbody c)
        {
            RB = c;
        }

        public Rigidbody RB { get; }

        public Object Target => RB;

        public bool IsEnabled => !RB.isKinematic;

        public bool LogChange => false;

        public string PropertyName => "!isKinematic";

        public bool SetEnabled(bool enabled)
        {
            if (enabled)
                return RB.UnsetKinematic();
            else
                return RB.SetKinematic();
        }
        public bool Equals(IEnabled other) => other is NonKinematic r && r.RB == RB;

    }
}