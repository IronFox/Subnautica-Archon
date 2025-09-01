using UnityEngine;

namespace Assets.Behavior.Util.Enabled
{
    /// <summary>
    /// Enables or disables forceRenderingOff on a Renderer.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching property name.")]
    internal class ForceRenderingOff : IEnabled
    {
        private readonly Renderer x;

        public ForceRenderingOff(Renderer x)
        {
            this.x = x;
        }

        public Object Target => x;

        public bool IsEnabled => x.forceRenderingOff;

        public bool LogChange => false;

        public string PropertyName => "forceRenderingOff";

        public bool Equals(IEnabled other) => other is ForceRenderingOff r && r.x == x;

        public void SetEnabled(bool enabled)
        {
            x.forceRenderingOff = enabled;
        }
    }
}