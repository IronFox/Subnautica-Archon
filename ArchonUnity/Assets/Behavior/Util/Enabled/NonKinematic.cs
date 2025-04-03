using UnityEditor;
using UnityEngine;

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

    public void SetEnabled(bool enabled)
    {
        if (enabled)
            RB.UnsetKinematic();
        else
            RB.SetKinematic();
    }
    public bool Equals(IEnabled other) => other is NonKinematic r && r.RB == RB;

}