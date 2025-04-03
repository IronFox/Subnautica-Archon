using UnityEngine;

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

    public void SetEnabled(bool enabled)
    {
        c.detectCollisions = enabled;
    }
    public bool Equals(IEnabled other) => other is CollisionsEnabled r && r.c == c;

}