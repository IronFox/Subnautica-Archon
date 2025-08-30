using Nautilus.Json;
using Nautilus.Options.Attributes;
using Subnautica_Archon;
using Subnautica_Archon.Util;
using UnityEngine;

[Menu("Archon")]
public class ArchonConfig : ConfigFile
{
    [Choice("Interior Lights",
        "Option.InteriorLights.Full",
        "Option.InteriorLights.Reduced",
        "Option.InteriorLights.Minimal",
        LabelLanguageId = "Option.InteriorLights"
    )]
    public InteriorLights interiorLights = InteriorLights.Full;
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
    [Slider(DefaultValue = 100, Format = "{0:F0} %", Label = "Voice Volume", LabelLanguageId = "Option.VoiceVolume", Min = 0, Max = 100, Step = 5)]
    public float voiceVolumePercent = 100;
    [Toggle("Show voice subtitles", LabelLanguageId = "Option.ShowVoiceSubtitles")]
    public bool showVoiceSubtitles = false;
    [Toggle("Default to First Person", LabelLanguageId = "Option.DefaultToFirstPerson")]
    public bool defaultToFirstPerson = false;
    [Choice("Autopilot voice",
        "Option.AutopilotVoice.Off",
        "Option.AutopilotVoice.Eve",
        LabelLanguageId = "Option.AutopilotVoice"
    )]
    public Voice voice = Voice.Eve;

    [Slider(DefaultValue = 100.0f, Format = "{0:F0}%", Label = "Look Sensitivity", LabelLanguageId = "Option.LookSensitivity", Min = 1f, Max = 200.0f, Step = 1f)]
    public float lookSensitivity = 100.0f;
    //[Toggle("Hold Sprint to Boost")]
    //public bool holdToBoost = false;

}