using System.Collections.Generic;
using UnityEngine;

namespace Behavior.Util
{

    public class BoundedRange
    {
        public float Min { get; private set; }
        public float Max { get; private set; }

        public static BoundedRange Empty { get; } = new BoundedRange(float.MaxValue, float.MinValue);

        public BoundedRange(float min, float max)
            => (Min, Max) = (min, max);
        
        public float Size => Max - Min;
        public float Center => (Min + Max) / 2f;
        
        public bool IsEmpty => Min > Max;

        public void Include(float value)
        {
            Min = Mathf.Min(Min, value);
            Max = Mathf.Max(Max, value);
        }
        public void Include(BoundedRange other)
        {
            if (other.IsEmpty)
                return;
            Min = Mathf.Min(Min, other.Min);
            Max = Mathf.Max(Max, other.Max);
        }
        
        public bool Contains(float value)
            => value >= Min && value <= Max;
        public bool Overlaps(BoundedRange other)
        {
            return other.Min <= Max && other.Max >= Min;
        }
    }

    public class Bounds2
    {
        public BoundedRange X { get; }
        public BoundedRange Y { get; }
        
        public Vector2 Min => new Vector2(X.Min, Y.Min);
        public Vector2 Max => new Vector2(X.Max, Y.Max);
        public Vector2 Center => new Vector2(X.Center, Y.Center);
        public Vector2 Size => new Vector2(X.Size, Y.Size);
        public float Area => X.Size * Y.Size;
        public Bounds2(BoundedRange x, BoundedRange y)
            => (X, Y) = (x, y);
        public Bounds2(float xMin, float xMax, float yMin, float yMax)
            : this(new BoundedRange(xMin, xMax), new BoundedRange(yMin, yMax))
        {
        }
        
        public static Bounds2 Empty => new Bounds2(BoundedRange.Empty, BoundedRange.Empty);


        public Bounds2 Include(Vector2 v)
        {
            X.Include(v.x);
            Y.Include(v.y);
            return this;
        }
        
        public Bounds2 Include(IEnumerable<Vector2> vertices)
        {
            foreach (var v in vertices)
                Include(v);
            return this;
        }

        public static Bounds2 From(IEnumerable<Vector2> vertices)
        {
            return Empty.Include(vertices);
        }
    }
}