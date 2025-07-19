using UnityEngine;

public abstract class DebugHandTarget : MonoBehaviour
{
    public abstract void OnTrigger(ArchonControl archon, FpsTest player);
    public virtual void OnHandOver(ArchonControl archon, FpsTest player)
    { }
}
