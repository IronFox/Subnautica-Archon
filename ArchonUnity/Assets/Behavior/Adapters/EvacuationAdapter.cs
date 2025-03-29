using System;
using UnityEngine;

public static class EvacuationAdapter
{
    public static Func<GameObject, bool> Predicate { get; set; } = obj => !obj.GetComponent<FpsTest>();

}

