using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAuthenticationResponse : VTSResponse<VTSAuthenticationResponseData>;
public sealed class VTSAuthenticationResponseData : VTSResponseData
{
    [JsonPropertyName("authenticated")] public required bool Authenticated { get; set; }
    [JsonPropertyName("reason")] public required string? Reason { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        AppendLine(b, prefix, Authenticated);
        AppendLine(b, prefix, Reason);
        return base.ToString(b, prefix);
    }
}