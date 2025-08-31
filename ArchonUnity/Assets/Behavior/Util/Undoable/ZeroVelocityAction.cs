using Assets.Behavior.Adapters;
using UnityEngine;

internal class ZeroVelocityAction : CommonAction<Rigidbody>
{
    public ZeroVelocityAction(Rigidbody c) : base(c)
    { }

    protected override bool ClientDo()
    {
        if (TypedTarget.velocity.sqrMagnitude > 0)
        {
            using (var log = Log.New())
            {
                log.Write($"Clearing velocity of {TypedTarget.NiceName()}");

                TypedTarget.velocity = Vector3.zero;
                return true;
            }
        }
        return false;
    }

    protected override void ClientUndo()
    { }
}