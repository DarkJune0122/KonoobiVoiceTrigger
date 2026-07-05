using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAPIStateResponse : VTSResponse<VTSAPIStateResposeData>;
public sealed class VTSAPIStateResposeData : VTSResponseData
{
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("vTubeStudioVersion")] public string? VTubeStudioVersion { get; set; }
    [JsonPropertyName("currentSessionAuthenticated")] public bool CurrentSessionAuthenticated { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        base.ToString(b, prefix).AppendLine();
        AppendLine(b, prefix, Active);
        AppendLine(b, prefix, VTubeStudioVersion);
        Append(b, prefix, CurrentSessionAuthenticated);
        return b;
    }
}