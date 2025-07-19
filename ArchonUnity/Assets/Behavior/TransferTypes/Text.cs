using UnityEngine;

namespace Assets.Behavior.TransferTypes
{
    public readonly struct Text
    {
        public enum Severity
        {
            Info,
            Warning,
            Error
        }


        public string Value { get; }
        public Color Color { get; }
        public Severity Level { get; }
        public Text(string value, Color color, Severity level = Severity.Info)
        {
            Value = value;
            Color = color;
            Level = level;
        }

        public static Text Info(string value) => new Text(value, Color.white, Severity.Info);
        public static Text Warning(string value) => new Text(value, Color.yellow, Severity.Warning);
        public static Text Error(string value) => new Text(value, Color.red, Severity.Error);

    }
}
