using Assets.Behavior.Adapters;
using Assets.Behavior.Util.Enabled;

namespace Assets.Behavior.Util.Undoable
{
    internal class SwitchAction : IAction
    {
        public SwitchAction(IEnabled c, bool toEnabled)
        {
            ToEnabled = toEnabled;
            Enabled = c;
            TargetName = c.Target.NiceName();
        }
        public string TargetName { get; }
        public bool ToEnabled { get; }
        public IEnabled Enabled { get; }
        public UnityEngine.Object Target => Enabled.Target;

        public bool TargetIsGone => Enabled is null || !Enabled.Target;
        private bool HaveLoggedGone { get; set; }
        public bool Do()
        {
            using (var log = Log.NewLazy())
            {

                if (!Enabled.Target)
                {
                    if (!HaveLoggedGone)
                    {
                        log.Warn($"Cannot set {Enabled.PropertyName} on {TargetName}: target is gone");
                        HaveLoggedGone = true;
                    }
                    return false;
                }
                if (Enabled.IsEnabled != ToEnabled)
                {
                    if (Enabled.LogChange)
                    {
                        log.Write($"Setting {Enabled.PropertyName} := {ToEnabled} on {TargetName}");
                    }

                    Enabled.SetEnabled(ToEnabled);
                    return true;
                }
                return false;
            }
        }

        public bool Equals(IAction other) => other is SwitchAction e && e.ToEnabled == ToEnabled && e.Enabled.Equals(Enabled);

        public void Undo()
        {
            using (var log = Log.NewLazy())
            {
                if (!Enabled.Target)
                {
                    if (!HaveLoggedGone)
                    {
                        log.Warn($"Cannot revert {Enabled.PropertyName} on {TargetName}: target is gone");

                        HaveLoggedGone = true;
                    }
                    return;
                }
                if (Enabled.IsEnabled == ToEnabled)
                {
                    if (Enabled.LogChange)
                    {
                        log.Write($"Resetting {Enabled.PropertyName} := {!ToEnabled} on {TargetName}");
                    }

                    Enabled.SetEnabled(!ToEnabled);
                }
            }
        }
    }
}
