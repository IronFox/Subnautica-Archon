
using System;
using UnityEngine;

public static class DockingAdapter
{
    public static Func<GameObject, IDockable> ToDockable { get; set; } = go => go.GetComponent<IDockable>();

}