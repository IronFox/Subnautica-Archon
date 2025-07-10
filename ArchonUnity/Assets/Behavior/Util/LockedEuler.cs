using System;
using UnityEngine;

/// <summary>
/// Euler angles with a locked Z component. Vertical rotation (around the X axis) is limited to [MinX,MaxX],
/// horizontal (around the Y axis) is limited to [0,360]
/// </summary>
public readonly struct LockedEuler
{
    /// <summary>
    /// Rotation around the X axis in [MinX, MaxX]
    /// </summary>
    public float X { get; }
    /// <summary>
    /// Rotation around the Y axis in [0,360]
    /// </summary>
    public float Y { get; }

    public TransformLocality Locality { get; }

    public const float MinX = -88f;
    public const float MaxX = 88f;

    public override string ToString() => $"({X.ToStr()},{Y.ToStr()})";

    public LockedEuler(float x, float y, TransformLocality locality)
    {
        X = x;
        Y = y;
        Locality = locality;
    }
    private static float SanitizeX(float x)
    {
        if (x == float.NaN)
            return 0;
        if (x == float.PositiveInfinity)
            return MaxX;
        if (x == float.NegativeInfinity)
            return MinX;
        while (x > 180)
            x -= 360;
        while (x < -180)
            x += 360;
        return Mathf.Clamp(x, MinX, MaxX);
    }

    private static float SanitizeY(float y)
    {
        if (y == float.NaN || y == float.PositiveInfinity || y == float.NegativeInfinity)
            return 0;
        return Mathf.Repeat(y, 360);
    }

    private static float Constraint(float value, System.Func<float, float> constraint, System.Func<float, float> sanitizer)
    {
        if (constraint == null)
            return sanitizer(value);
        var constrained = constraint(value);
        if (constrained == float.NaN || constrained == float.PositiveInfinity || constrained == float.NegativeInfinity)
            return sanitizer(value);
        return sanitizer(constrained);
    }

    public LockedEuler ConstrainedHead()
        => Locality == TransformLocality.Local
            ? Constrained(angle => Mathf.Clamp(angle, -70, 70), angle => angle > 90 && angle < 180
                                ? 90
                                : angle > 180 && angle < 270 ? 270
                                : angle)
        : throw new InvalidOperationException("ConstrainedHead can only be used with Local locality");

    public LockedEuler Constrained(
        System.Func<float, float> xConstraint = null,
        System.Func<float, float> yConstraint = null)
    {
        return new LockedEuler(
            Constraint(X, xConstraint, SanitizeX),
            Constraint(Y, yConstraint, SanitizeY),
            Locality);
    }

    public LockedEuler RotateBy(float x, float y)
    {
        return new LockedEuler(SanitizeX(x + X), SanitizeY(y + Y), Locality);
    }

    public LockedEuler RotateBy(float x, float y, float factor)
        => RotateBy(x * factor, y * factor);

    public void ApplyTo(Transform target)
    {
        switch (Locality)
        {
            case TransformLocality.Global:
                target.eulerAngles = Vector;
                break;
            case TransformLocality.Local:
                target.localEulerAngles = Vector;
                break;
        }
    }
    public static LockedEuler FromAngles(Vector3 e, TransformLocality locality)
    {

        return new LockedEuler(SanitizeX(e.x), SanitizeY(e.y), locality);

    }
    public static LockedEuler FromGlobal(Transform target)
        => FromAngles(target.eulerAngles, TransformLocality.Global);
    public static LockedEuler FromLocal(Transform target)
        => FromAngles(target.localEulerAngles, TransformLocality.Local);
    public static LockedEuler From(Quaternion q, TransformLocality locality)
        => FromAngles(q.eulerAngles, locality);

    public Vector3 Forward => Quaternion * Vector3.forward;
    public Vector3 Right => Quaternion * Vector3.right;
    public Vector3 Up => Quaternion * Vector3.up;
    public Quaternion Quaternion => Quaternion.Euler(X, Y, 0);
    public Vector3 Vector => new Vector3(X, Y, 0);

    public static LockedEuler Slerp(LockedEuler x, LockedEuler y, float t)
        => From(Quaternion.Slerp(x.Quaternion, y.Quaternion, t), x.Locality);


    public static LockedEuler FromForward(Vector3 forward, TransformLocality locality)
        => FromAngles(Quaternion.FromToRotation(Vector3.forward, forward).eulerAngles, locality);

};