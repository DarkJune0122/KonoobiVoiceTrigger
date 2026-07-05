using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAuthenticationTokenResponse : VTSResponse<VTSAuthenticationTokenResponseData>;
public sealed class VTSAuthenticationTokenResponseData : VTSResponseData
{
    [JsonPropertyName("authenticationToken")] public string? AuthenticationToken { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        base.ToString(b, prefix).AppendLine();
        AppendLine(b, prefix, AuthenticationToken);
        return b;
    }
}