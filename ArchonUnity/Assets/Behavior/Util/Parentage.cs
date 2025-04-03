using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct Parentage
{
    public Location Transform { get; }
    public Transform Parent { get; }
    public Transform Target { get; }

    public Parentage(Location transform, Transform parent, Transform target)
    {
        Transform = transform;
        Parent = parent;
        Target = target;
    }

    public static Parentage FromLocal(Transform t)
        => new Parentage(Location.FromLocal(t), t.parent, t);

    public void Restore()
    {
        if (Target)
        {
            Target.parent = Parent;
            Transform.ApplyTo(Target);
        }
    }
}
