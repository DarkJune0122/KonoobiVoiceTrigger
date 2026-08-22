using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSTestEvent : VTSResponse<VTSTestEventData>;
public sealed class VTSTestEventData : VTSResponseData
{
    [JsonPropertyName("yourTestMessage")] public required string? YourTestMessage { get; init; }
    [JsonPropertyName("counter")] public required int Counter { get; init; }

    public override StringBuilder ToString(StringBuilder b, string prefix = "")
    {
        base.ToString(b, prefix);
        AppendLine(b, prefix, YourTestMessage);
        Append(b, prefix, Counter);
        return b;
    }
}
