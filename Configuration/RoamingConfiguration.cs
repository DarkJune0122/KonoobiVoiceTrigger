using System.Text.Json.Serialization;

namespace VoiceTrigger.Configuration;

public sealed class RoamingConfiguration : ConfigurationTemplate
{
    [JsonIgnore] protected override string FilePath => RoamingConfigurationFilePath;

    [JsonInclude] public double ActivationPower { get; set; } = 1;
    [JsonInclude] public double NormalActivationDuration { get; set; } = 7;
    [JsonInclude] public double TriggeredReleaseDuration { get; set; } = 60;
    [JsonInclude] public double NormalActivationJump { get; set; } = 0.05;
    [JsonInclude] public double TriggeredActivationJump { get; set; } = 0.02;
    [JsonInclude] public int SelectedResistanceIndex { get; set; } = -1;
    [JsonInclude] public int SelectedSamplingRateIndex { get; set; } = -1;
    [JsonInclude] public AudioDeviceDescriptor? SelectedAudioDevice { get; set; }
    [JsonInclude] public ModelHotkey? SelectedHotkey { get; set; }
    [JsonInclude] public ModelExpression? SelectedExpression { get; set; }
    [JsonInclude] public bool EnableFreezing { get; set; } = true;
    [JsonInclude] public double FreezeDuration { get; set; } = 90;
    [JsonInclude] public bool InstantUnfreezeOnManualNormal { get; set; } = true;
    //[JsonInclude] public bool AllowUnfreezeWhileNormal { get; set; } = true;
    //[JsonInclude] public double NormalUnfreezeDelay { get; set; } = 15;
    //[JsonInclude] public bool AllowUnfreezeWhileTriggered { get; set; } = true;
    //[JsonInclude] public double TriggeredUnfreezeDelay { get; set; } = 30;
    [JsonInclude] public double AuraFrameRate { get; set; } = 90;
    [JsonInclude] public double AuraFrequency { get; set; } = 0.5;
}
