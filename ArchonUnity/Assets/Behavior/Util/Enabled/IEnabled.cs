using System;

public interface IEnabled : IEquatable<IEnabled>
{
    UnityEngine.Object Target { get; }
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
    bool LogChange { get; }

    string PropertyName { get; }
}