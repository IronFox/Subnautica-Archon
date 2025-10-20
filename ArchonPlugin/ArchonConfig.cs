using AVS.Configuration;
using AVS.Log;
using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Options.Attributes;
using Newtonsoft.Json;
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
    [JsonIgnore]
    [BindableButton("Toggle Free Camera", LabelLocalizationKey = "Option.Button.ToggleFreeCamera", KeyboardDefault = GameInputHandler.Paths.Keyboard.F)]
    public GameInput.Button toggleFreeCamera;
    [JsonIgnore]
    [BindableButton("Change 3rd Person Camera Height", LabelLocalizationKey = "Option.Button.ChangeExternalCameraHeight", KeyboardDefault = GameInputHandler.Paths.Keyboard.V)]
    public GameInput.Button btnChangeExternalCameraHeight;
    [JsonIgnore]
    [BindableButton("Reduce the 3rd Person Camera", LabelLocalizationKey = "Option.Button.AltZoomIn")]
    public GameInput.Button btnAltZoomIn;
    [JsonIgnore]
    [BindableButton("Increase the 3rd Person Camera", LabelLocalizationKey = "Option.Button.AltZoomOut")]
    public GameInput.Button btnAltZoomOut;
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

    [Slider(DefaultValue = 100.0f, Format = "{0:F0}%", Label = "Look Sensitivity", LabelLanguageId = "Option.LookSensitivity", Min = 1f, Max = 200f, Step = 1f)]
    public float lookSensitivity = 100.0f;

    [Slider(DefaultValue = 100f, Format = "{0:F0}%", Label = "Engine Sound Volume", LabelLanguageId = "Option.EngineSoundVolume", Min = 0f, Max = 100f, Step = 5f)]
    public float engineSoundVolume = 100f;

    [Slider(DefaultValue = 100f, Format = "{0:F0}%", Label = "Bioreactor Sound Volume", LabelLanguageId = "Option.BioreactorSoundVolume", Min = 0f, Max = 100f, Step = 5f)]
    public float bioreactorSoundVolume = 100f;

    [Choice("Log Level",
        "Option.LogVerbosity.Verbose",
        "Option.LogVerbosity.Regular",
        "Option.LogVerbosity.WarningsAndErrorsOnly",
        LabelLanguageId = "Option.LogVerbosity"
    )]
    public Verbosity logLevel = Verbosity.Regular;

}