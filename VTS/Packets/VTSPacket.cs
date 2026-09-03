using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSPacket : VTSPacketData // Inherits associated functions.
{
    protected const string DefaultAPIName = "VTubeStudioPublicAPI";
    protected const string DefaultAPIVersion = "1.0";

    [JsonPropertyName(VTSPackets.APINameJsonPropertyName)] public virtual string? APIName { get; set; }
    [JsonPropertyName(VTSPackets.APIVersionJsonPropertyName)] public virtual string? APIVersion { get; set; }
    [JsonPropertyName(VTSPackets.MessageTypeJsonPropertyName)] public virtual string? MessageType { get; set; }
    [JsonPropertyName(VTSPackets.RequestIDJsonPropertyName)] public virtual string? RequestID { get; set; }

    public override void Reset()
    {
        base.Reset();
        APIName = default;
        APIVersion = default;
        MessageType = default;
        RequestID = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    AppendLine(b, prefix, APIName);
    //    AppendLine(b, prefix, APIVersion);
    //    AppendLine(b, prefix, MessageType);
    //    Append(b, prefix, RequestID);
    //    return b;
    //}
}
