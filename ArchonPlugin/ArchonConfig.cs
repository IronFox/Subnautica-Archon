using Nautilus.Json;
using Nautilus.Options.Attributes;
using UnityEngine;

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
    [Slider(DefaultValue = 100, Format = "{0:F0} %", Label = "Voice Volume", LabelLanguageId = "optVoiceVolume", Min = 0, Max = 100, Step = 5)]
    public float voiceVolumePercent = 100;
    [Toggle("Show voice subtitles", LabelLanguageId = "optShowVoiceSubtitles")]
    public bool showVoiceSubtitles = false;

    //[Toggle("Hold Sprint to Boost")]
    //public bool holdToBoost = false;

}