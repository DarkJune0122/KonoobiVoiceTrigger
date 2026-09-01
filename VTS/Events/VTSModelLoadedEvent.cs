using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSModelLoadedEvent : VTSResponse<VTSModelLoadedEventData>;
public sealed class VTSModelLoadedEventData : VTSResponseData
{
    [JsonPropertyName("modelLoaded")] public required bool ModelLoaded { get; init; }
    [JsonPropertyName("modelName")] public required string? ModelName { get; init; }
    [JsonPropertyName("modelID")] public required string? ModelID { get; init; }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, ModelLoaded);
    //    AppendLine(b, prefix, ModelName);
    //    Append(b, prefix, ModelID);
    //    return b;
    //}
}