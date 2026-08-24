using System.Text.Json.Serialization;
using VoiceTrigger.Services;

namespace VoiceTrigger.Configuration;

public sealed class RoamingConfiguration : ConfigurationTemplate
{
    [JsonIgnore] protected override string FilePath => ConfigurationService.RoamingConfigurationFilePath;

    [JsonInclude] public double ActivationPower { get; set; } = 1;
    [JsonInclude] public double NormalActivationDuration { get; set; } = 6;
    [JsonInclude] public double TriggeredReleaseDuration { get; set; } = 48;
    [JsonInclude] public double NormalActivationJump { get; set; } = 0.05;
    [JsonInclude] public double TriggeredActivationJump { get; set; } = 0.025;
    [JsonInclude] public int SelectedResistanceIndex { get; set; } = -1;
    [JsonInclude] public int SelectedSamplingRateIndex { get; set; } = -1;
    [JsonInclude] public string SelectedAudioDeviceID { get; set; } = string.Empty;
    [JsonInclude] public ModelHotkey? SelectedHotkey { get; set; }
    [JsonInclude] public ModelExpression? SelectedExpression { get; set; }
    [JsonInclude] public bool EnableFreezing { get; set; } = false; // Debug.
    [JsonInclude] public bool AllowUnfreezeWhileNormal { get; set; } = true;
    [JsonInclude] public double NormalUnfreezeDelay { get; set; } = 15;
    [JsonInclude] public bool AllowUnfreezeWhileTriggered { get; set; } = true;
    [JsonInclude] public double TriggeredUnfreezeDelay { get; set; } = 30;
    [JsonInclude] public double AuraFrameRate { get; set; } = 90;
    [JsonInclude] public double AuraFrequency { get; set; } = 0.5;
}
