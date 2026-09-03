using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSTestEvent : VTSResponse<VTSTestEventData>;
public sealed class VTSTestEventData : VTSResponseData
{
    [JsonPropertyName("yourTestMessage")] public required string? YourTestMessage { get; set; }
    [JsonPropertyName("counter")] public required int Counter { get; set; }

    public override void Reset()
    {
        base.Reset();
        YourTestMessage = default;
        Counter = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, YourTestMessage);
    //    Append(b, prefix, Counter);
    //    return b;
    //}
}
