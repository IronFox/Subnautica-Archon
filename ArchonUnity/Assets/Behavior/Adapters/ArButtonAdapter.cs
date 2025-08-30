using System;

namespace Assets.Behavior.Adapters
{
    public static class ArButtonAdapter
    {
        public static Action<ArButton> Instrument { get; set; } = btn => { };
    }
}
