using UnityEngine;

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

    public void SetEnabled(bool enabled)
    {
        x.enabled = enabled;
    }
    public bool Equals(IEnabled other) => other is BehaviourEnabled r && r.x == x;

}