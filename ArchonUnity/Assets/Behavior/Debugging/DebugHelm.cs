using UnityEngine;

public class DebugHelm : DebugHandTarget
{
    public Transform exit;
    public override void OnTrigger(ArchonControl archon, FpsTest player)
    {
        player.EnterHelm(archon, this);
    }

}
