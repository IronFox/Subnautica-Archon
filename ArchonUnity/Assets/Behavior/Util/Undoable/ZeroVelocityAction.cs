using UnityEngine;

internal class ZeroVelocityAction : CommonAction<Rigidbody>
{
    public ZeroVelocityAction(Rigidbody c) : base(c)
    {}

    protected override bool ClientDo()
    {
        if (TypedTarget.velocity.sqrMagnitude > 0)
        {
            LogConfig.Default.Write($"Clearing velocity of {TypedTarget.NiceName()}");

            TypedTarget.velocity = Vector3.zero;
            return true;
        }
        return false;
    }

    protected override void ClientUndo()
    {}
}