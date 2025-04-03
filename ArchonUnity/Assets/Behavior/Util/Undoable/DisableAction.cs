using UnityEditor;
using UnityEngine;

internal class DisableAction : IAction
{
    public DisableAction(IEnabled c)
    {
        Enabled = c;
    }

    public IEnabled Enabled { get; }
    public Object Target => Enabled.Target;


    public bool Do()
    {
        if (Enabled.IsEnabled)
        {
            if (Enabled.LogChange)
                LogConfig.Default.Write($"Setting {Enabled.PropertyName}:=false on {Enabled.Target.NiceName()} [{Enabled.Target.GetInstanceID()}]");
            Enabled.SetEnabled(false);
            return true;
        }
        return false;
    }

    public bool Equals(IAction other) => other is DisableAction d && d.Enabled.Equals(Enabled);

    public void Undo()
    {
        if (!Enabled.IsEnabled)
        {
            if (Enabled.LogChange)
                LogConfig.Default.Write($"Setting {Enabled.PropertyName}:=true on {Enabled.Target.NiceName()} [{Enabled.Target.GetInstanceID()}]");
            Enabled.SetEnabled(true);
        };
    }
}