using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSDiscoveryResponse : VTSResponse<VTSDiscoveryResponseData>;
public sealed class VTSDiscoveryResponseData : VTSResponseData
{
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("port")] public ushort Port { get; set; }
    [JsonPropertyName("instanceID")] public string? InstanceID { get; set; }
    [JsonPropertyName("windowTitle")] public string? WindowTitle { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        AppendLine(b, prefix, Active);
        AppendLine(b, prefix, Port);
        AppendLine(b, prefix, InstanceID);
        AppendLine(b, prefix, WindowTitle);
        return base.ToString(b, prefix);
        
    }
}
