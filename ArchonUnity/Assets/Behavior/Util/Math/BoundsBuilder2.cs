using System.Collections.Generic;
using UnityEngine;

namespace Behavior.Util.Math
{

    public class BoundsBuilder2
    {
        public BoundedRangeBuilder X { get; }
        public BoundedRangeBuilder Y { get; }
        public int Id { get; }
        
        private static int idCounter = 0;

        public Vector2 Min => new Vector2(X.Min, Y.Min);
        public Vector2 Max => new Vector2(X.Max, Y.Max);
        public Vector2 Center => new Vector2(X.Center, Y.Center);
        public Vector2 Size => new Vector2(X.Size, Y.Size);
        public float Area => X.Size * Y.Size;
        public Bounds2 Baked => new Bounds2(X.Range, Y.Range);

        public BoundsBuilder2(BoundedRangeBuilder x, BoundedRangeBuilder y)
        {
            (X, Y) = (x, y);
            Id = idCounter++;
        }

        public BoundsBuilder2(float xMin, float xMax, float yMin, float yMax)
            : this(new BoundedRangeBuilder(xMin, xMax), new BoundedRangeBuilder(yMin, yMax))
        {}
        
        public override string ToString()
        => $"#{Id}({Min}, {Max})";
        
        public static BoundsBuilder2 Empty => new BoundsBuilder2(BoundedRangeBuilder.Empty, BoundedRangeBuilder.Empty);


        public BoundsBuilder2 Include(Vector2 v)
        {
            X.Include(v.x);
            Y.Include(v.y);
            return this;
        }
        
        public BoundsBuilder2 Include(IEnumerable<Vector2> vertices)
        {
            foreach (var v in vertices)
                Include(v);
            return this;
        }

        public static BoundsBuilder2 From(IEnumerable<Vector2> vertices)
        {
            return Empty.Include(vertices);
        }
    }
}