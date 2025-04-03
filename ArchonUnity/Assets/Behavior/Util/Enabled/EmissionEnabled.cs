using UnityEngine;

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

    public void SetEnabled(bool enabled)
    {
        var em = x.emission;
        em.enabled = enabled;
    }
    public bool Equals(IEnabled other) => other is EmissionEnabled r && r.x == x;
}