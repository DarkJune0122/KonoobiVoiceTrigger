using System.Text.Json.Serialization;
using VoiceTrigger.Services;

namespace VoiceTrigger.Configuration;

public sealed class LocalConfiguration : ConfigurationTemplate
{
    [JsonIgnore] protected override string FilePath => ConfigurationService.LocalConfigurationFilePath;

}
