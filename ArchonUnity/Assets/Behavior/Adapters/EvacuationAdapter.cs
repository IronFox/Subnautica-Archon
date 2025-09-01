using Assets.Behavior.Debugging;
using System;
using UnityEngine;

namespace Assets.Behavior.Adapters
{
    public static class EvacuationAdapter
    {
        public static Func<GameObject, bool> ShouldEvacuate { get; set; } = obj => !obj.GetComponent<FpsTest>();
        public static Func<GameObject, bool> ShouldKeep { get; set; } = obj => !!obj.GetComponent<FpsTest>();

    }


}