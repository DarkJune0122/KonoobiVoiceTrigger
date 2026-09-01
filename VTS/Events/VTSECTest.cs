using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSECTest : VTSEventConfig
{
    [JsonPropertyName("testMessageForEvent")] public required string? TestMessageForEvent { get; set; }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    Append(b, prefix, TestMessageForEvent);
    //    return b;
    //}
}
