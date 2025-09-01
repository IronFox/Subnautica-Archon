using System;

namespace Assets.Behavior.Util.Enabled
{
    /// <summary>
    /// Interface for components that can be enabled or disabled, with support for logging and equality comparison.
    /// </summary>
    public interface IEnabled : IEquatable<IEnabled>
    {
        UnityEngine.Object Target { get; }
        bool IsEnabled { get; }
        void SetEnabled(bool enabled);
        bool LogChange { get; }

        string PropertyName { get; }
    }
}