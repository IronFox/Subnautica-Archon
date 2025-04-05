using UnityEngine;

public abstract class CommonAction<T> : IAction  where T : Object
{
    protected T TypedTarget { get; }
    public string TargetName { get; }
    public CommonAction(T c)
    {
        TypedTarget = c;
        TargetName = c.NiceName();
    }

    public Object Target => TypedTarget;

    public bool TargetIsGone => !TypedTarget;
    private bool HaveLoggedGone { get; set; }

    protected bool RequireTarget()
    {
        if (TargetIsGone)
        {
            if (!HaveLoggedGone)
            {
                LogConfig.Default.LogWarning($"Cannot execute {GetType().Name} operation on {TargetName}: target is gone");
                HaveLoggedGone = true;
            }
            return false;
        }
        return true;
    }
    public bool Do()
    {
        if (TargetIsGone)
        {
            if (!HaveLoggedGone)
            {
                LogConfig.Default.LogWarning($"Cannot execute operation {GetType().Name}.Do() on {TargetName}: target is gone");
                HaveLoggedGone = true;
            }
            return false;
        }
        return ClientDo();
    }

    protected abstract bool ClientDo();
    protected abstract void ClientUndo();

    public void Undo()
    {
        if (TargetIsGone)
        {
            if (!HaveLoggedGone)
            {
                LogConfig.Default.LogWarning($"Cannot execute operation {GetType().Name}.Undo() on {TargetName}: target is gone");
                HaveLoggedGone = true;
            }
            return;
        }
        ClientUndo();
    }

    public bool Equals(IAction other)
        => other is CommonAction<T> c
        && c.GetType() == this.GetType()
        && c.TargetName == this.TargetName;
}