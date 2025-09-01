using UnityEngine;

namespace Assets.Behavior.Debugging
{
    /// <summary>
    /// Base class for things the player can interact with using their hand in debug mode.
    /// </summary>
    public abstract class DebugHandTarget : MonoBehaviour
    {
        public abstract void OnTrigger(ArchonControl archon, FpsTest player);
        public virtual void OnHandOver(ArchonControl archon, FpsTest player)
        { }
    }

}