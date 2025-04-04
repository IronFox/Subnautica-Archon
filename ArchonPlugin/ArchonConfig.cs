using Nautilus.Options.Attributes;
using Nautilus.Json;
using UnityEngine;
using System;

[Menu("Archon Options")]
public class ArchonConfig : ConfigFile
{
    [Keybind("Toggle Free Camera ")]
    public KeyCode toggleFreeCamera = KeyCode.F;
    [Keybind("Reduce the 3rd Person Camera")]
    public KeyCode altZoomIn = KeyCode.None;
    [Keybind("Increase the 3rd Person Camera")]
    public KeyCode altZoomOut = KeyCode.None;
    [Toggle("Flip Free Horizontal Rotation in Reverse")]
    public bool flipFreeHorizontalRotationInReverse = true;
    [Toggle("Flip Free Vertical Rotation in Reverse")]
    public bool flipFreeVerticalRotationInReverse = false;
    [Toggle("Default to Free Camera")]
    public bool defaultToFreeCamera = true;

    //[Toggle("Hold Sprint to Boost")]
    //public bool holdToBoost = false;

}