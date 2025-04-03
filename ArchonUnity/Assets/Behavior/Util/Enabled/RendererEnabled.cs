using UnityEngine;

internal class RendererEnabled : IEnabled
{
    private readonly Renderer x;

    public RendererEnabled(Renderer x)
    {
        this.x = x;
    }

    public Object Target => x;

    public bool IsEnabled => x.enabled;

    public bool LogChange => false;

    public string PropertyName => "enabled";

    public bool Equals(IEnabled other) => other is RendererEnabled r && r.x == x;

    public void SetEnabled(bool enabled)
    {
        x.enabled = enabled;
    }
}