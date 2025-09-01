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
        /// <summary>
        /// Changes the enabled state.
        /// </summary>
        /// <param name="enabled">New value to set to</param>
        /// <returns>True if the enable state was different and changed, false otherwise</returns>
        bool SetEnabled(bool enabled);
        bool LogChange { get; }

        string PropertyName { get; }
    }
}