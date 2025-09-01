using System;
using UnityEngine;

public readonly struct Location
{
    public static Location LocalIdentity { get; } = new Location(FullEuler.LocalIdentity, Vector3.zero, null);
    public static Location GlobalIdentity { get; } = new Location(FullEuler.GlobalIdentity, Vector3.zero, null);

    public Location(FullEuler rotation, Vector3 position, Transform localRelativeTo) : this()
    {
        Euler = rotation;
        Position = position;
        LocalOrigin = localRelativeTo;
    }

    public override string ToString() => $"@{Position},r={Euler},l={Locality}";

    public FullEuler Euler { get; }
    public Vector3 Position { get; }
    /// <summary>
    /// Original transform this local descriptor is relative to, or null if global
    /// </summary>
    public Transform LocalOrigin { get; }

    public TransformLocality Locality => Euler.Locality;
    public static Location FromLocal(Transform source)
        => new Location(FullEuler.FromLocal(source), position: source.localPosition, source.parent);
    public static Location FromLocal(GameObject source)
        => FromLocal(source.transform);
    public static Location FromGlobal(Transform source)
        => new Location(FullEuler.FromGlobal(source), position: source.position, null);
    public static Location FromGlobal(GameObject source)
        => FromGlobal(source.transform);

    public void ApplyTo(GameObject target)
        => ApplyTo(target.transform);
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

    public static Location Lerp(Location a, Location b, float t)
        => new Location(FullEuler.Slerp(a.Euler, b.Euler, t), Vector3.Lerp(a.Position, b.Position, t), null);

    /// <summary>
    /// Transforms this global descriptor to a local descriptor in the given transform
    /// </summary>
    /// <param name="transform">Transform to localize in</param>
    /// <returns>Localized descriptor</returns>
    /// <exception cref="InvalidOperationException">If the local descriptor was not global</exception>
    public Location Localize(Transform transform)
    {
        if (Locality != TransformLocality.Global)
            throw new InvalidOperationException($"{nameof(Location)} has locality {Locality}. Needs Global");
        Quaternion q
            = Quaternion.Inverse(transform.rotation)
            * Euler.Quaternion;

        return new Location(
            FullEuler.FromAngles(
                q.eulerAngles,
                TransformLocality.Local
            ),
            transform.InverseTransformPoint(Position),
            transform
        );
    }
    /// <summary>
    /// Transforms this local descriptor to a global descriptor using the given transform
    /// </summary>
    /// <param name="transform">Transform to globalize with</param>
    /// <returns>Globalized descriptor</returns>
    /// <exception cref="InvalidOperationException">If the local descriptor was not local</exception>
    public Location Globalize()
    {
        if (Locality != TransformLocality.Local)
            throw new InvalidOperationException($"{nameof(Location)} has locality {Locality}. Needs Local");
        if (LocalOrigin == null)
            throw new InvalidOperationException($"{nameof(Location)} has no {nameof(LocalOrigin)}");
        Quaternion q
            = LocalOrigin.rotation
            * Euler.Quaternion;

        return new Location(
            FullEuler.FromAngles(
                q.eulerAngles,
                TransformLocality.Global
            ),
            LocalOrigin.TransformPoint(Position),
            null
        );
    }


    /// <summary>
    /// Produces a transformed version where the rotation is replaced with the given global rotation
    /// </summary>
    /// <param name="localTransform">Transform to localize into IF the local descriptor is <see cref="TransformLocality.Local"></see></param>
    /// <param name="globalRotation">Global rotation to set</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Location WithGlobalRotation(Transform localTransform, Quaternion globalRotation)
    {
        switch (Locality)
        {
            case TransformLocality.Global:
                return new Location(FullEuler.From(globalRotation, TransformLocality.Global), Position, null);
            case TransformLocality.Local:
                {
                    var local = Quaternion.Inverse(localTransform.rotation) * globalRotation;
                    return new Location(FullEuler.From(local, TransformLocality.Local), Position, localTransform);
                }
            default:
                throw new InvalidOperationException($"Unexpected locality: {Locality}");
        }
    }

    public Location TranslatedBy(Vector3 delta)
        => new Location(Euler, Position + delta, LocalOrigin);

    internal Location RotatedBy(Quaternion rotation)
        => new Location(Euler.RotateBy(rotation), /*rotation * */Position, LocalOrigin);
}
