
using System;
using UnityEngine;

public static class DockingAdapter
{
    public enum Filter
    {
        All,
        CurrentlyDockable,
        CurrentlyDockedBySaveGame
    }
    public static Func<GameObject, ArchonControl, Filter, IDockable> ToDockable { get; set; } =
        (go, ctrl, filter) =>
            filter == DockingAdapter.Filter.CurrentlyDockedBySaveGame
                ? null
                : go.GetComponent<IDockable>();

}

