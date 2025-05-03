using System;
using UnityEngine;

public static class EvacuationAdapter
{
    public static Func<GameObject, bool> ShouldEvacuate { get; set; } = obj => !obj.GetComponent<FpsTest>();
    public static Func<GameObject, bool> ShouldKeep { get; set; } = obj => !!obj.GetComponent<FpsTest>();

}

