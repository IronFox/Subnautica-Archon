using System;
using System.Collections.Generic;
using UnityEngine;


namespace Behavior.Util.Math
{
    public readonly struct Bounds2 : IEquatable<Bounds2>
    {
        public BoundsRange X { get; }
        public BoundsRange Y { get; }
        
        public Bounds2(BoundsRange x, BoundsRange y)
        {
            X = x;
            Y = y;
        }

        public Vector2 Center => new Vector2(X.Center, Y.Center);
        public Vector2 Size => new Vector2(X.Size, Y.Size);
        
        public Vector2 Min => new Vector2(X.Min, Y.Min);
        public Vector2 Max => new Vector2(X.Max, Y.Max);
        
        public override string ToString() => $"Bounds2 @{Center} s={Size}";

        public bool Equals(Bounds2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is Bounds2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static Bounds2 From(IEnumerable<Vector2> vertices)
            => BoundsBuilder2.From(vertices).Baked;
    }
}