using System;

namespace Behavior.Util.Math
{
    /// <summary>
    /// Represents a range of floating-point values defined by a minimum and maximum boundary.
    /// </summary>
    /// <remarks>
    /// A <see cref="BoundsRange"/> is an immutable structure used to describe a contiguous range of
    /// values. It provides properties for analyzing the range, such as size, center, and whether
    /// the range is empty. Methods for checking if a value or another range overlaps with this
    /// instance are also available.
    /// </remarks>
    /// <seealso cref="System.IEquatable{T}"/>
    public readonly struct BoundsRange : IEquatable<BoundsRange>
    {
        public float Min { get; }
        public float Max { get; }
        
        public float Size => Max - Min;
        public float Center => (Min + Max) / 2f;
        
        public bool IsEmpty => Min > Max;
        public BoundsRange(float min, float max)
        {
            Min = min;
            Max = max;
        }
        
        public bool Contains(float value)
            => value >= Min && value <= Max;
        public bool Contains(BoundsRange other)
            => other.Min >= Min && other.Max <= Max;

        /// <summary>
        /// Determines whether the given <see cref="BoundsRange"/> is fully contained within this <see cref="BoundsRange"/>, assuming it is centered within this range.
        /// </summary>
        /// <param name="other">The range to check for containment.</param>
        /// <returns>True if the specified range would fit while located in the same center, otherwise false.</returns>
        public bool ContainsCentered(BoundsRange other)
            => other.Size <= Size;

        public bool Overlaps(BoundedRangeBuilder other)
        {
            return other.Min <= Max && other.Max >= Min;
        }
        
        public override string ToString() => IsEmpty ? "<empty>" : $"[{Min}, {Max}]";

        public bool Equals(BoundsRange other) => Min.Equals(other.Min) && Max.Equals(other.Max);

        public override bool Equals(object obj) => obj is BoundsRange other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Min, Max);

        public BoundsRange TranslatedBy(float delta) => new BoundsRange(Min + delta, Max + delta);


        /// <summary>
        /// Creates a <see cref="BoundsRange"/> instance that is centered on the specified value and has the specified size.
        /// </summary>
        /// <param name="center">The central point of the range.</param>
        /// <param name="size">The total size or width of the range. Must be a non-negative value.</param>
        /// <returns>A new <see cref="BoundsRange"/> instance centered at the specified value with the given size.</returns>
        public static BoundsRange Centered(float center, float size)
        {
            return new BoundsRange(center - size / 2f, center + size / 2f);
        }
    }
}