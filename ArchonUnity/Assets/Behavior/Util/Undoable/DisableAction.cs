using Assets.Behavior.Util.Enabled;

namespace Assets.Behavior.Util.Undoable
{
    internal class DisableAction : SwitchAction
    {
        public DisableAction(IEnabled c) : base(c, toEnabled: false)
        { }
    }
}