using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAPIStateRequest : VTSRequest
{
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "APIStateRequest";
}
