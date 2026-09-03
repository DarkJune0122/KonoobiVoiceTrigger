using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAuthenticationResponse : VTSResponse<VTSAuthenticationResponseData>;
public sealed class VTSAuthenticationResponseData : VTSResponseData
{
    [JsonPropertyName("authenticated")] public bool Authenticated { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }

    public override void Reset()
    {
        base.Reset();
        Authenticated = default;
        Reason = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    AppendLine(b, prefix, Authenticated);
    //    AppendLine(b, prefix, Reason);
    //    return base.ToString(b, prefix);
    //}
}