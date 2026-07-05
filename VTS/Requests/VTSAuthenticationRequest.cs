using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAuthenticationRequest : VTSRequest<VTSAuthenticationRequestData>
{
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "AuthenticationRequest";
}
public sealed class VTSAuthenticationRequestData : VTSRequestData
{
    [JsonPropertyName("pluginName")] public required string? PluginName { get; set; }
    [JsonPropertyName("pluginDeveloper")] public required string? PluginDeveloper { get; set; }
    [JsonPropertyName("authenticationToken")] public required string? AuthenticationToken { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        base.ToString(b, prefix);
        AppendLine(b, prefix, PluginName);
        AppendLine(b, prefix, PluginDeveloper);
        Append(b, prefix, AuthenticationToken);
        return b;
    }
}