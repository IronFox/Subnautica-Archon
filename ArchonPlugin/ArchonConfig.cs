using Nautilus.Json;
using Nautilus.Options.Attributes;
using Subnautica_Archon;
using UnityEngine;

[Menu("Archon Options")]
public class ArchonConfig : ConfigFile
{
    [Choice("Flood light shadows. May impact performance",
        "Option.FloodLightShadows.None",
        "Option.FloodLightShadows.Hard",
        "Option.FloodLightShadows.Soft",
        LabelLanguageId = "Option.FloodLightShadows"
    )]
    public LightShadows floodLightShadows = LightShadows.Hard;
    [Keybind("Toggle Free Camera", LabelLanguageId = "Option.Button.ToggleFreeCamera")]
    public KeyCode toggleFreeCamera = KeyCode.F;
    [Keybind("Change 3rd Person Camera Height", LabelLanguageId = "Option.Button.ChangeExternalCameraHeight")]
    public KeyCode btnChangeExternalCameraHeight = KeyCode.V;
    [Keybind("Reduce the 3rd Person Camera", LabelLanguageId = "Option.Button.AltZoomIn")]
    public KeyCode btnAltZoomIn = KeyCode.None;
    [Keybind("Increase the 3rd Person Camera", LabelLanguageId = "Option.Button.AltZoomOut")]
    public KeyCode btnAltZoomOut = KeyCode.None;
    [Toggle("Flip Free Horizontal Rotation in Reverse", LabelLanguageId = "Option.FlipFreeHorizontalRotationInReverse")]
    public bool flipFreeHorizontalRotationInReverse = true;
    [Toggle("Flip Free Vertical Rotation in Reverse", LabelLanguageId = "Option.FlipFreeVerticalRotationInReverse")]
    public bool flipFreeVerticalRotationInReverse = false;
    [Toggle("Default to Cockpit Camera", LabelLanguageId = "Option.DefaultToFreeCameraInExternalCamera")]
    public bool defaultToCockpit = true;
    [Slider(DefaultValue = 100, Format = "{0:F0} %", Label = "Voice Volume", LabelLanguageId = "Option.VoiceVolume", Min = 0, Max = 100, Step = 5)]
    public float voiceVolumePercent = 100;
    [Toggle("Show voice subtitles", LabelLanguageId = "Option.ShowVoiceSubtitles")]
    public bool showVoiceSubtitles = false;
    [Choice("Autopilot voice",
        "Option.AutopilotVoice.Off",
        "Option.AutopilotVoice.Default",
        LabelLanguageId = "Option.AutopilotVoice"
    )]
    public Voice voice = Voice.Default;
    //[Toggle("Hold Sprint to Boost")]
    //public bool holdToBoost = false;

}