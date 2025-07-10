using UnityEngine;

public abstract class DebugHandTarget : MonoBehaviour
{
    public abstract void OnTrigger(ArchonControl archon, FpsTest player);
}
