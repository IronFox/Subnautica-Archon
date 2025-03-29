using System;
using UnityEngine;

public readonly struct Sphere : IEquatable<Sphere>
{
    public Vector3 Center { get; }
    public float Radius { get; }

    public Sphere(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public override bool Equals(object obj)
    {
        return obj is Sphere sphere &&
               Center.Equals(sphere.Center) &&
               Radius == sphere.Radius;
    }

    public override int GetHashCode()
    {
        int hashCode = 1641483799;
        hashCode = hashCode * -1521134295 + Center.GetHashCode();
        hashCode = hashCode * -1521134295 + Radius.GetHashCode();
        return hashCode;
    }

    public override string ToString() => $"Sphere @{Center} r{Radius}";

    public bool Equals(Sphere other) => Center.Equals(other.Center) && Radius == other.Radius;
}