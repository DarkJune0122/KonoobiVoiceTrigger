using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAuthenticationTokenRequest : VTSRequest<VTSAuthenticationTokenRequestData>
{
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "AuthenticationTokenRequest";
}
public sealed class VTSAuthenticationTokenRequestData : VTSRequestData
{
    [JsonPropertyName("pluginName")] public required string? PluginName { get; set; }
    [JsonPropertyName("pluginDeveloper")] public required string? PluginDeveloper { get; set; }
    [JsonPropertyName("pluginIcon")] public required string? PluginIcon { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        base.ToString(b, prefix);
        AppendLine(b, prefix, PluginName);
        AppendLine(b, prefix, PluginDeveloper);
        Append(b, prefix, PluginIcon);
        return b;
    }
}