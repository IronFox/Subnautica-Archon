using UnityEngine;

public class DebugHatch : DebugHandTarget
{
    private Transform exit, entry;

    public override void OnTrigger(ArchonControl archon, FpsTest player)
    {
        if (!archon.IsBoarded)
            Board(player, archon);
        else
            Exit(player, archon);

    }

    private void Board(FpsTest player, ArchonControl subControl)
    {
        subControl.Enter(player.ToReference());
        player.OnBoard(entry.position);
    }

    private void Exit(FpsTest player, ArchonControl subControl)
    {
        player.OnExit(exit.position);
        subControl.Exit();
    }

    // Start is called before the first frame update
    void Start()
    {
        exit = transform.Find("Exit");
        entry = transform.Find("Entry");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
