using System.Text;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSPacket : VTSPacketData // Inherits associated functions.
{
    protected const string DefaultAPIName = "VTubeStudioPublicAPI";
    protected const string DefaultAPIVersion = "1.0";

    [JsonPropertyName("apiName")] public virtual string? APIName { get; set; }
    [JsonPropertyName("apiVersion")] public virtual string? APIVersion { get; set; }
    [JsonPropertyName("messageType")] public virtual string? MessageType { get; set; }
    [JsonPropertyName("requestID")] public virtual string? RequestID { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        AppendLine(b, prefix, APIName);
        AppendLine(b, prefix, APIVersion);
        AppendLine(b, prefix, MessageType);
        Append(b, prefix, RequestID);
        return b;
    }
}
