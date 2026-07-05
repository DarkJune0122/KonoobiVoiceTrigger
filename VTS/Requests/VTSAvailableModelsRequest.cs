using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAvailableModelsRequest : VTSRequest
{
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "AvailableModelsRequest";
}
