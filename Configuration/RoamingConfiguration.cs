using System.Text.Json.Serialization;
using VoiceTrigger.Services;

namespace VoiceTrigger.Configuration;

public sealed class RoamingConfiguration : ConfigurationTemplate
{
    [JsonIgnore] protected override string FilePath => ConfigurationService.RoamingConfigurationFilePath;

}
