namespace Subnautica_Archon.Util
{
    public class Range
    {
        public float Min { get; set; }
        public float Max { get; set; }
        public Range(float min, float max)
        {
            Min = min;
            Max = max;
        }
        public Range(float value)
        {
            Min = value;
            Max = value;
        }
        public static Range Empty => new Range(float.MaxValue, float.MinValue);
        public void Include(float value)
        {
            if (value < Min)
                Min = value;
            if (value > Max)
                Max = value;
        }
        public bool Contains(float value)
        {
            return value >= Min && value <= Max;
        }
        public override string ToString()
        {
            return $"Range({Min}, {Max})";
        }
    }
}
