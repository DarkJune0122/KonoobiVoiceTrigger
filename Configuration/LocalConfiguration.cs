using System.Text.Json.Serialization;
using VoiceTrigger.Audio;

namespace VoiceTrigger.Configuration;

public sealed class LocalConfiguration : ConfigurationTemplate
{
    [JsonIgnore] protected override string FilePath => LocalConfigurationFilePath;

    [JsonInclude] public bool IsAudioCaptureActive { get; set; } = false;
    [JsonInclude] public int SelectedSamplingRateIndex { get; set; } = -1;
    [JsonInclude] public AudioDeviceDescriptor? SelectedAudioDevice { get; set; }
    //[JsonInclude] public bool StartHidden { get; set; } = false;
}
