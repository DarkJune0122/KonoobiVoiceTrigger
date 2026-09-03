using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSModelHotkeysRequest : VTSRequest<VTSModelHotkeysRequestData>
{
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "HotkeysInCurrentModelRequest";
}
public sealed class VTSModelHotkeysRequestData : VTSRequestData
{
    [JsonPropertyName("modelID")] public required string? ModelID { get; set; }
    [JsonPropertyName("live2DItemFileName")] public required string? Live2DItemFileName { get; set; }

    public override void Reset()
    {
        base.Reset();
        ModelID = default;
        Live2DItemFileName = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, ModelID);
    //    Append(b, prefix, Live2DItemFileName);
    //    return b;
    //}
}