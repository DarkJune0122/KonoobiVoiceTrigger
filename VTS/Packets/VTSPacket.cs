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

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix, bool newLine = DefaultNewLine)
    {
        Append(b, prefix, nameof(APIName), APIName);
        Append(b, prefix, nameof(APIVersion), APIVersion);
        Append(b, prefix, nameof(MessageType), MessageType);
        Append(b, prefix, nameof(RequestID), RequestID, newLine);
        return b;
    }
}
