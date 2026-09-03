using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;
using VTS.Core;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSDiscoveryResponse : VTSResponse<VTSDiscoveryResponseData>;
public sealed class VTSDiscoveryResponseData : VTSResponseData
{
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("port")] public ushort Port { get; set; }
    [JsonPropertyName("instanceID")] public string? InstanceID { get; set; }
    [JsonPropertyName("windowTitle")] public string? WindowTitle { get; set; }

    public override void Reset()
    {
        base.Reset();
        Active = default;
        Port = default;
        InstanceID = default;
        WindowTitle = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    AppendLine(b, prefix, Active);
    //    AppendLine(b, prefix, Port);
    //    AppendLine(b, prefix, InstanceID);
    //    AppendLine(b, prefix, WindowTitle);
    //    return base.ToString(b, prefix);
    //}
}
