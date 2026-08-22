using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSCurrentModelResponse : VTSResponse<VTSCurrentModelsResponseData>;
public sealed class VTSCurrentModelsResponseData : VTSResponseData
{
    [JsonPropertyName("modelLoaded")] public required bool ModelLoaded { get; set; }
    [JsonPropertyName("modelName")] public required string? ModelName { get; set; }
    [JsonPropertyName("modelID")] public required string? ModelID { get; set; }
    [JsonPropertyName("vtsModelName")] public required string? VTSModelName { get; set; }
    [JsonPropertyName("vtsModelIconName")] public required string? VTSModelIconName { get; set; }
    [JsonPropertyName("live2DModelName")] public required string? Live2DModelName { get; set; }
    [JsonPropertyName("modelLoadTime")] public required int ModelLoadTime { get; set; }
    [JsonPropertyName("timeSinceModelLoaded")] public required int TimeSinceModelLoaded { get; set; }
    [JsonPropertyName("numberOfLive2DParameters")] public required int NumberOfLive2DParameters { get; set; }
    [JsonPropertyName("numberOfLive2DArtmeshes")] public required int NumberOfLive2DArtmeshes { get; set; }
    [JsonPropertyName("hasPhysicsFile")] public required bool HasPhysicsFile { get; set; }
    [JsonPropertyName("numberOfTextures")] public required int NumberOfTextures { get; set; }
    [JsonPropertyName("textureResolution")] public required int TextureResolution { get; set; }
    [JsonPropertyName("modelPosition")] public required Position ModelPosition { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = "")
    {
        AppendLine(b, prefix, ModelLoaded);
        AppendLine(b, prefix, ModelName);
        AppendLine(b, prefix, ModelID);
        AppendLine(b, prefix, VTSModelName);
        AppendLine(b, prefix, VTSModelIconName);
        AppendLine(b, prefix, Live2DModelName);
        AppendLine(b, prefix, ModelLoadTime);
        AppendLine(b, prefix, TimeSinceModelLoaded);
        AppendLine(b, prefix, NumberOfLive2DParameters);
        AppendLine(b, prefix, NumberOfLive2DArtmeshes);
        AppendLine(b, prefix, HasPhysicsFile);
        AppendLine(b, prefix, NumberOfTextures);
        AppendLine(b, prefix, TextureResolution);
        AppendData(b, prefix, ModelPosition);
        return base.ToString(b, prefix);
    }

    public readonly struct Position
    {
        [JsonPropertyName("positionX")] public required float PositionX { get; init; }
        [JsonPropertyName("positionY")] public required float PositionY { get; init; }
        [JsonPropertyName("rotation")] public required float Rotation { get; init; }
        [JsonPropertyName("size")] public required float Size { get; init; }

        public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
        public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
        public static StringBuilder ToString(Position pos, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => pos.ToString(b, prefix);
        public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
        {
            VTSHelpers.AppendLine(b, prefix, PositionX);
            VTSHelpers.AppendLine(b, prefix, PositionY);
            VTSHelpers.AppendLine(b, prefix, Rotation);
            VTSHelpers.Append(b, prefix, Size);
            return b;
        }
    }
}