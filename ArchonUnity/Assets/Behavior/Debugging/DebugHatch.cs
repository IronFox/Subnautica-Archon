using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugHatch : MonoBehaviour
{
    private Transform exit, entry;


    internal void Board(Rigidbody player, ArchonControl subControl)
    {
        subControl.Enter(player.gameObject);
        player.useGravity = true;
        player.transform.position = entry.position;
    }

    internal void Exit(Rigidbody player, ArchonControl subControl)
    {
        player.transform.position = exit.position;
        player.useGravity = false;
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
