using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSEventSubscriptionResponse : VTSResponse<VTSEventSubscriptionResponseData>;
public sealed class VTSEventSubscriptionResponseData : VTSResponseData
{
    [JsonPropertyName("subscribedEventCount")] public required int SubscribedEventCount { get; init; }
    [JsonPropertyName("subscribedEvents")] public required string[]? SubscribedEvents { get; init; }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, SubscribedEventCount);
    //    AppendList(b, prefix, SubscribedEvents, VTSHelpers.StringWriter);
    //    return b;
    //}
}
