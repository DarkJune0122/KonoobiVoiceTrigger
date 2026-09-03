using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAvailableModelsResponse : VTSResponse<VTSAvailableModelsResponseData>;
public sealed class VTSAvailableModelsResponseData : VTSResponseData
{
    [JsonPropertyName("numberOfModels")] public int NumberOfModels { get; set; }
    [JsonPropertyName("availableModels")] public List<Model>? AvailableModels { get; set; }

    public override void Reset()
    {
        base.Reset();
        NumberOfModels = default;
        AvailableModels?.Clear();
    }

    public readonly struct Model
    {
        [JsonPropertyName("modelLoaded")] public bool ModelLoaded { get; init; }
        [JsonPropertyName("modelName")] public string? ModelName { get; init; }
        [JsonPropertyName("modelID")] public string? ModelID { get; init; }
        [JsonPropertyName("vtsModelName")] public string? VTSModelName { get; init; }
        [JsonPropertyName("vtsModelIconName")] public string? VTSModelIconName { get; init; }

        public override string ToString() => VTSPackets.ToLoggingString(this);
        //public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
        //public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
        //public static StringBuilder ToString(Model model, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => model.ToString(b, prefix);
        //public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
        //{
        //    VTSHelpers.AppendLine(b, prefix, ModelLoaded);
        //    VTSHelpers.AppendLine(b, prefix, ModelName);
        //    VTSHelpers.AppendLine(b, prefix, ModelID);
        //    VTSHelpers.AppendLine(b, prefix, VTSModelName);
        //    VTSHelpers.Append(b, prefix, VTSModelIconName);
        //    return b;
        //}
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    AppendLine(b, prefix, NumberOfModels);
    //    AppendList(b, prefix, AvailableModels, Model.ToString).AppendLine();
    //    return base.ToString(b, prefix);
    //}
}
