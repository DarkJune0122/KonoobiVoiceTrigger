using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSEventSubscriptionRequest : VTSRequest<VTSEventSubscriptionRequestData>
{
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "EventSubscriptionRequest";
}
public sealed class VTSEventSubscriptionRequestData : VTSRequestData
{
    [JsonPropertyName("eventName")] public required string? EventName { get; set; }
    [JsonPropertyName("subscribe")] public required bool Subscribe { get; set; }
    [JsonPropertyName("config")] public required VTSEventConfig? Config { get; set; }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, EventName);
    //    AppendLine(b, prefix, Subscribe);
    //    Append(b, prefix, Config);
    //    return b;
    //}
}