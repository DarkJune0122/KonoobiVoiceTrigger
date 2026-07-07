using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAvailableModelsRequest : VTSRequest
{
    public static readonly VTSAvailableModelsRequest Instance = new();
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "AvailableModelsRequest";
}
