
using System;
using UnityEngine;

public static class DockingAdapter
{
    public static Func<GameObject, ArchonControl, IDockable> ToDockable { get; set; } = (go,ctrl) => go.GetComponent<IDockable>();

}