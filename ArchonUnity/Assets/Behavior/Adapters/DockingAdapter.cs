
using System;
using UnityEngine;

public static class DockingAdapter
{
    public enum Filter
    {
        All,
        CurrentlyDockable
    }
    public static Func<GameObject, ArchonControl, Filter, IDockable> ToDockable { get; set; } = (go,ctrl, filter) => go.GetComponent<IDockable>();

}

