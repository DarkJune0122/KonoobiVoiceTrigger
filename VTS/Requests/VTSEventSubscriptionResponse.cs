using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;
using VTS.Core;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSEventSubscriptionResponse : VTSResponse<VTSEventSubscriptionResponseData>;
public sealed class VTSEventSubscriptionResponseData : VTSResponseData
{
    [JsonPropertyName("subscribedEventCount")] public int SubscribedEventCount { get; set; }
    [JsonPropertyName("subscribedEvents")] public List<string>? SubscribedEvents { get; set; }

    public override void Reset()
    {
        base.Reset();
        SubscribedEventCount = default;
        SubscribedEvents?.Clear();
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, SubscribedEventCount);
    //    AppendList(b, prefix, SubscribedEvents, VTSHelpers.StringWriter);
    //    return b;
    //}
}
