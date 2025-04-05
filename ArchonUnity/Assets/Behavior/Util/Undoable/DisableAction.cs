using UnityEditor;
using UnityEngine;

internal class DisableAction : IAction
{
    public DisableAction(IEnabled c)
    {
        Enabled = c;
        TargetName = c.Target.NiceName();
    }
    public string TargetName { get; }
    public IEnabled Enabled { get; }
    public Object Target => Enabled.Target;

    public bool TargetIsGone => Enabled is null || !Enabled.Target;
    private bool HaveLoggedGone { get; set; }
    public bool Do()
    {
        if (!Enabled.Target)
        {
            if (!HaveLoggedGone)
            {
                LogConfig.Default.LogWarning($"Cannot do {Enabled.PropertyName} on {TargetName}: target is gone");
                HaveLoggedGone = true;
            }
            return false;
        }
        if (Enabled.IsEnabled)
        {
            if (Enabled.LogChange)
                LogConfig.Default.Write($"Setting {Enabled.PropertyName}:=false on {Enabled.Target.NiceName()}");
            Enabled.SetEnabled(false);
            return true;
        }
        return false;
    }

    public bool Equals(IAction other) => other is DisableAction d && d.Enabled.Equals(Enabled);

    public void Undo()
    {
        if (!Enabled.Target)
        {
            if (!HaveLoggedGone)
            {
                LogConfig.Default.LogWarning($"Cannot undo {Enabled.PropertyName} on {TargetName}: target is gone");
                HaveLoggedGone = true;
            }
            return;
        }
        if (!Enabled.IsEnabled)
        {
            if (Enabled.LogChange)
                LogConfig.Default.Write($"Setting {Enabled.PropertyName}:=true on {Enabled.Target.NiceName()} [{Enabled.Target.GetInstanceID()}]");
            Enabled.SetEnabled(true);
        };
    }
}