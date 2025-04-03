using UnityEngine;

internal class ZeroVelocityAction : IAction
{
    private Rigidbody RB { get; }

    public ZeroVelocityAction(Rigidbody c)
    {
        this.RB = c;
    }

    public Object Target => RB;

    public bool Do()
    {
        if (RB.velocity.sqrMagnitude > 0)
        {
            LogConfig.Default.Write($"Clearing velocity of [{RB}]");

            RB.velocity = Vector3.zero;
            return true;
        }
        return false;
    }

    public void Undo()
    {
    }

    public bool Equals(IAction other) => other is ZeroVelocityAction z && RB == z.RB;
}