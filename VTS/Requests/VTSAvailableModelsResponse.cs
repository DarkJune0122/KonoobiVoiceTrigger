using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAvailableModelsResponse : VTSResponse<VTSAvailableModelsResponseData>;
public sealed class VTSAvailableModelsResponseData : VTSResponseData
{
    [JsonPropertyName("numberOfModels")] public required int NumberOfModels { get; set; }
    [JsonPropertyName("availableModels")] public required Model[]? AvailableModels { get; set; }

    public readonly struct Model
    {
        [JsonPropertyName("modelLoaded")] public required bool ModelLoaded { get; init; }
        [JsonPropertyName("modelName")] public required string? ModelName { get; init; }
        [JsonPropertyName("modelID")] public required string? ModelID { get; init; }
        [JsonPropertyName("vtsModelName")] public required string? VTSModelName { get; init; }
        [JsonPropertyName("vtsModelIconName")] public required string? VTSModelIconName { get; init; }

        public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
        public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
        public static StringBuilder ToString(Model model, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => model.ToString(b, prefix);
        public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
        {
            VTSHelpers.AppendLine(b, prefix, ModelLoaded);
            VTSHelpers.AppendLine(b, prefix, ModelName);
            VTSHelpers.AppendLine(b, prefix, ModelID);
            VTSHelpers.AppendLine(b, prefix, VTSModelName);
            VTSHelpers.Append(b, prefix, VTSModelIconName);
            return b;
        }
    }

    public override StringBuilder ToString(StringBuilder b, string prefix = "")
    {
        AppendLine(b, prefix, NumberOfModels);
        AppendList(b, prefix, AvailableModels, Model.ToString).AppendLine();
        return base.ToString(b, prefix);
    }
}
