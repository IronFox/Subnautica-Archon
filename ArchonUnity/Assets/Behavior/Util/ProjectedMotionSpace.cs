using System;
using UnityEngine;

/// <summary>
/// Captures the intended sub motion one second in the future
/// </summary>
public class ProjectedMotionSpace
{
    public ProjectedMotionSpace(Vector3 center)
    {
        Center = center;
    }
    private ProjectedMotionSpace(Vector3 center, Vector3 motion, Quaternion rotation)
    {
        Center = center;
        Motion = motion;
        Rotation = rotation;
    }

    public Vector3 Center { get; }
    public Vector3 Motion { get; }
    public Quaternion Rotation { get; private set; } = Quaternion.identity;

    /// <summary>
    /// projects the given point one second into the future
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public Vector3 Predict(Vector3 p)
    {
        var relative = p - Center;
        var rotated = Rotation * relative;
        return Center + rotated + Motion;
    }

    public void RotateThisBy(Vector3 axis, float angle)
    {
        Rotation = Quaternion.AngleAxis(angle, axis) * Rotation;
    }

    public ProjectedMotionSpace TranslateByRelative(Transform context, Vector3 motion)
    {
        var abs = context.TransformDirection(motion);
        return new ProjectedMotionSpace(Center, Motion + motion, Rotation);
    }

    internal ProjectedMotionSpace TranslateBy(Vector3 velocity, bool flip)
    {
        return new ProjectedMotionSpace(Center,
            Motion + velocity,
            flip ? Quaternion.Inverse(Rotation) : Rotation);
    }
}