using Assets.Behavior.Util.Enabled;

namespace Assets.Behavior.Util.Undoable
{
    internal class EnableAction : SwitchAction
    {
        public EnableAction(IEnabled c) : base(c, toEnabled: true)
        { }
    }
}
