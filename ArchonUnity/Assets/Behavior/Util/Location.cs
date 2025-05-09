﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public readonly struct Location 
{
    public static Location LocalIdentity { get; } = new Location(FullEuler.LocalIdentity, Vector3.zero);
    public static Location GlobalIdentity { get; } = new Location(FullEuler.GlobalIdentity, Vector3.zero);

    public Location(FullEuler rotation, Vector3 position) : this()
    {
        Euler = rotation;
        Position = position;
    }

    public FullEuler Euler { get; }
    public Vector3 Position { get; }

    public TransformLocality Locality => Euler.Locality;
    public static Location FromLocal(Transform source)
        => new Location(FullEuler.FromLocal(source), position: source.localPosition);
    public static Location FromLocal(GameObject source)
        => FromLocal(source.transform);
    public static Location FromGlobal(Transform source)
        => new Location(FullEuler.FromGlobal(source), position: source.position);
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
        => new Location(FullEuler.Slerp(a.Euler, b.Euler, t), Vector3.Lerp(a.Position,b.Position,t));

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
            transform.InverseTransformPoint(Position)
        );
    }
    /// <summary>
    /// Transforms this local descriptor to a global descriptor using the given transform
    /// </summary>
    /// <param name="transform">Transform to globalize with</param>
    /// <returns>Globalized descriptor</returns>
    /// <exception cref="InvalidOperationException">If the local descriptor was not local</exception>
    public Location Globalize(Transform transform)
    {
        if (Locality != TransformLocality.Local)
            throw new InvalidOperationException($"{nameof(Location)} has locality {Locality}. Needs Local");
        Quaternion q
            = transform.rotation
            * Euler.Quaternion;

        return new Location(
            FullEuler.FromAngles(
                q.eulerAngles,
                TransformLocality.Global
            ),
            transform.TransformPoint(Position)
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
                return new Location(FullEuler.From(globalRotation, TransformLocality.Global), Position);
            case TransformLocality.Local:
                {
                    var local = Quaternion.Inverse(localTransform.rotation) * globalRotation;
                    return new Location(FullEuler.From(local, TransformLocality.Local), Position);
                }
            default:
                throw new InvalidOperationException($"Unexpected locality: {Locality}");
        }
    }

    public Location TranslatedBy(Vector3 delta)
        => new Location(Euler, Position + delta);

    internal Location RotatedBy(Quaternion rotation)
        => new Location(Euler.RotateBy(rotation), /*rotation * */Position);
}
