using Assets.Behavior.Debugging;
using System;

namespace Assets.Behavior.Adapters
{
    public static class ArButtonAdapter
    {
        public static Action<ArchonControl, ArButton> Instrument { get; set; } = (ctrl, btn) =>
        {
            if (!btn.GetComponent<DebugArButton>())
            {
                btn.gameObject.AddComponent<DebugArButton>();
                return;
            }

        };
    }
}
