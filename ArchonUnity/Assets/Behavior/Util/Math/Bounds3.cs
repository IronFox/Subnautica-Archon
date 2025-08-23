using System;
using UnityEngine;

namespace Behavior.Util.Math
{
    public readonly struct Bounds3 : IEquatable<Bounds3>
    {
        public BoundsRange X { get; }
        public BoundsRange Y { get; }
        public BoundsRange Z { get; }
        
        public Vector3 Center => new Vector3(X.Center, Y.Center, Z.Center);
        public Vector3 Size => new Vector3(X.Size, Y.Size, Z.Size);
        
        public Vector3 Min => new Vector3(X.Min, Y.Min, Z.Min);
        public Vector3 Max => new Vector3(X.Max, Y.Max, Z.Max);
        
        public Bounds3(BoundsRange x, BoundsRange y, BoundsRange z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public static Bounds3 From(Bounds b)
        {
            return new Bounds3(new BoundsRange(b.min.x, b.max.x), new BoundsRange(b.min.y, b.max.y), new BoundsRange(b.min.z, b.max.z));
        }
        
        public override string ToString() => $"Bounds3 @{Center} s={Size}";

        public bool Equals(Bounds3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        public override bool Equals(object obj) => obj is Bounds3 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public Bounds3 TranslatedBy(Vector3 delta) 
            => new Bounds3(
                X.TranslatedBy(delta.x),
                Y.TranslatedBy(delta.y),
                Z.TranslatedBy(delta.z)
                );

        public bool Contains(Bounds3 other)
            =>    X.Contains(other.X) 
               && Y.Contains(other.Y)
               && Z.Contains(other.Z);

        /// <summary>
        /// Determines whether the specified Bounds3 is fully contained within this Bounds3
        /// assuming it was at the same center.
        /// </summary>
        /// <param name="other">The Bounds3 instance to check for containment.</param>
        /// <returns>True if the specified Bounds3 is fully contained within this Bounds3, if it were located in the same center.</returns>
        public bool ContainsCentered(Bounds3 other)
            =>    X.ContainsCentered(other.X)
               && Y.ContainsCentered(other.Y)
               && Z.ContainsCentered(other.Z);

        public static Bounds3 CenterBox(Vector3 center, Vector3 size)
            => new Bounds3(
                BoundsRange.Centered(center.x, size.x),
                BoundsRange.Centered(center.y, size.y),
                BoundsRange.Centered(center.z, size.z)
            );
    }
}