using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSModelLoadedEvent : VTSResponse<VTSModelLoadedEventData>;
public sealed class VTSModelLoadedEventData : VTSResponseData
{
    [JsonPropertyName("modelLoaded")] public required bool ModelLoaded { get; set; }
    [JsonPropertyName("modelName")] public required string? ModelName { get; set; }
    [JsonPropertyName("modelID")] public required string? ModelID { get; set; }

    public override void Reset()
    {
        base.Reset();
        ModelLoaded = default;
        ModelName = default;
        ModelID = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, ModelLoaded);
    //    AppendLine(b, prefix, ModelName);
    //    Append(b, prefix, ModelID);
    //    return b;
    //}
}