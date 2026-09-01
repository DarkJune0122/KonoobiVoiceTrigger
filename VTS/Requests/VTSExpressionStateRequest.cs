using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSExpressionStateRequest : VTSRequest<VTSExpressionStateRequestData>
{
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "ExpressionStateRequest";
}
public sealed class VTSExpressionStateRequestData : VTSRequestData
{
    [JsonPropertyName("details")] public required bool Details { get; set; }
    [JsonPropertyName("expressionFile")] public required string? ExpressionFile { get; set; }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, Details);
    //    Append(b, prefix, ExpressionFile);
    //    return b;
    //}
}
