using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSCurrentModelRequest : VTSRequest
{
    public static readonly VTSCurrentModelRequest Instance = new();
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "CurrentModelRequest";
}