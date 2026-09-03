using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSCurrentModelResponse : VTSResponse<VTSCurrentModelsResponseData>;
public sealed class VTSCurrentModelsResponseData : VTSResponseData
{
    [JsonPropertyName("modelLoaded")] public bool ModelLoaded { get; set; }
    [JsonPropertyName("modelName")] public string? ModelName { get; set; }
    [JsonPropertyName("modelID")] public string? ModelID { get; set; }
    [JsonPropertyName("vtsModelName")] public string? VTSModelName { get; set; }
    [JsonPropertyName("vtsModelIconName")] public string? VTSModelIconName { get; set; }
    [JsonPropertyName("live2DModelName")] public string? Live2DModelName { get; set; }
    [JsonPropertyName("modelLoadTime")] public int ModelLoadTime { get; set; }
    [JsonPropertyName("timeSinceModelLoaded")] public int TimeSinceModelLoaded { get; set; }
    [JsonPropertyName("numberOfLive2DParameters")] public int NumberOfLive2DParameters { get; set; }
    [JsonPropertyName("numberOfLive2DArtmeshes")] public int NumberOfLive2DArtmeshes { get; set; }
    [JsonPropertyName("hasPhysicsFile")] public bool HasPhysicsFile { get; set; }
    [JsonPropertyName("numberOfTextures")] public int NumberOfTextures { get; set; }
    [JsonPropertyName("textureResolution")] public int TextureResolution { get; set; }
    [JsonPropertyName("modelPosition")] public Position ModelPosition { get; set; }

    public override void Reset()
    {
        base.Reset();
        ModelLoaded = default;
        ModelID = default;
        VTSModelName = default;
        VTSModelIconName = default;
        Live2DModelName = default;
        ModelLoadTime = default;
        TimeSinceModelLoaded = default;
        NumberOfLive2DParameters = default;
        NumberOfLive2DArtmeshes = default;
        HasPhysicsFile = default;
        NumberOfTextures = default;
        TextureResolution = default;
        ModelPosition = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    AppendLine(b, prefix, ModelLoaded);
    //    AppendLine(b, prefix, ModelName);
    //    AppendLine(b, prefix, ModelID);
    //    AppendLine(b, prefix, VTSModelName);
    //    AppendLine(b, prefix, VTSModelIconName);
    //    AppendLine(b, prefix, Live2DModelName);
    //    AppendLine(b, prefix, ModelLoadTime);
    //    AppendLine(b, prefix, TimeSinceModelLoaded);
    //    AppendLine(b, prefix, NumberOfLive2DParameters);
    //    AppendLine(b, prefix, NumberOfLive2DArtmeshes);
    //    AppendLine(b, prefix, HasPhysicsFile);
    //    AppendLine(b, prefix, NumberOfTextures);
    //    AppendLine(b, prefix, TextureResolution);
    //    AppendData(b, prefix, ModelPosition);
    //    return base.ToString(b, prefix);
    //}

    public readonly struct Position
    {
        [JsonPropertyName("positionX")] public float PositionX { get; init; }
        [JsonPropertyName("positionY")] public float PositionY { get; init; }
        [JsonPropertyName("rotation")] public float Rotation { get; init; }
        [JsonPropertyName("size")] public float Size { get; init; }

        public override string ToString() => VTSPackets.ToLoggingString(this);
        //public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
        //public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
        //public static StringBuilder ToString(Position pos, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => pos.ToString(b, prefix);
        //public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
        //{
        //    VTSHelpers.AppendLine(b, prefix, PositionX);
        //    VTSHelpers.AppendLine(b, prefix, PositionY);
        //    VTSHelpers.AppendLine(b, prefix, Rotation);
        //    VTSHelpers.Append(b, prefix, Size);
        //    return b;
        //}
    }
}