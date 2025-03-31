using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public readonly struct TransformDescriptor 
{
    public static TransformDescriptor LocalIdentity { get; } = new TransformDescriptor(FullEuler.LocalIdentity, Vector3.zero);
    public static TransformDescriptor GlobalIdentity { get; } = new TransformDescriptor(FullEuler.GlobalIdentity, Vector3.zero);

    public TransformDescriptor(FullEuler rotation, Vector3 position) : this()
    {
        Euler = rotation;
        Position = position;
    }

    public FullEuler Euler { get; }
    public Vector3 Position { get; }

    public TransformLocality Locality => Euler.Locality;
    public static TransformDescriptor FromLocal(Transform source)
        => new TransformDescriptor(FullEuler.FromLocal(source), position: source.localPosition);
    public static TransformDescriptor FromGlobal(Transform source)
        => new TransformDescriptor(FullEuler.FromGlobal(source), position: source.position);

    public void ApplyTo(Transform target)
    {
        Euler.ApplyTo(target);
        switch (Euler.Locality)
        {
            case TransformLocality.Local:
                target.localPosition = Position;
                break;
            case TransformLocality.Global:
                target.position = Position;
                break;
        }
    }

    public static TransformDescriptor Lerp(TransformDescriptor a, TransformDescriptor b, float t)
        => new TransformDescriptor(FullEuler.Slerp(a.Euler, b.Euler, t), Vector3.Lerp(a.Position,b.Position,t));

    /// <summary>
    /// Transforms this global descriptor to a local descriptor in the given transform
    /// </summary>
    /// <param name="transform">Transform to localize in</param>
    /// <returns>Localized descriptor</returns>
    /// <exception cref="InvalidOperationException">If the local descriptor was not global</exception>
    public TransformDescriptor Localize(Transform transform)
    {
        if (Locality != TransformLocality.Global)
            throw new InvalidOperationException($"{nameof(TransformDescriptor)} has locality {Locality}. Needs Global");
        Quaternion q
            = Quaternion.Inverse(transform.rotation)
            * Euler.Quaternion;

        return new TransformDescriptor(
            FullEuler.FromAngles(
                q.eulerAngles,
                TransformLocality.Local
            ),
            transform.InverseTransformPoint(Position)
        );
    }
    /// <summary>
    /// Transforms this local descriptor to a global descriptor using the given transform
    /// </summary>
    /// <param name="transform">Transform to globalize with</param>
    /// <returns>Globalized descriptor</returns>
    /// <exception cref="InvalidOperationException">If the local descriptor was not local</exception>
    public TransformDescriptor Globalize(Transform transform)
    {
        if (Locality != TransformLocality.Local)
            throw new InvalidOperationException($"{nameof(TransformDescriptor)} has locality {Locality}. Needs Local");
        Quaternion q
            = transform.rotation
            * Euler.Quaternion;

        return new TransformDescriptor(
            FullEuler.FromAngles(
                q.eulerAngles,
                TransformLocality.Global
            ),
            transform.TransformPoint(Position)
        );
    }
}
