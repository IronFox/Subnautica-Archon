using UnityEngine;

namespace Behavior.Util.Math
{
    /// <summary>
    /// Represents a utility class to construct and manage a bounded range of floating-point values.
    /// </summary>
    /// <remarks>
    /// A bounded range is defined by a minimum and maximum value. This class provides methods to
    /// calculate properties like size and center of the range, check its emptiness, and include
    /// additional values or other bounded ranges to dynamically extend its boundaries.
    /// </remarks>
    public class BoundedRangeBuilder
    {
        public float Min { get; private set; }
        public float Max { get; private set; }

        public static BoundedRangeBuilder Empty => new BoundedRangeBuilder(float.MaxValue, float.MinValue);

        public BoundedRangeBuilder(float min, float max)
            => (Min, Max) = (min, max);
        
        public float Size => Max - Min;
        public float Center => (Min + Max) / 2f;
        
        public bool IsEmpty => Min > Max;

        /// <summary>
        /// Gets the bounds of the current range as an instance of <see cref="BoundsRange"/>.
        /// </summary>
        /// <remarks>
        /// The range is defined by its minimum and maximum values. This property provides
        /// a compact representation of the current range.
        /// </remarks>
        public BoundsRange Range => new BoundsRange(Min, Max);

        public void Include(float value)
        {
            Min = Mathf.Min(Min, value);
            Max = Mathf.Max(Max, value);
        }
        public void Include(BoundedRangeBuilder other)
        {
            if (other.IsEmpty)
                return;
            Min = Mathf.Min(Min, other.Min);
            Max = Mathf.Max(Max, other.Max);
        }
        

    }

}