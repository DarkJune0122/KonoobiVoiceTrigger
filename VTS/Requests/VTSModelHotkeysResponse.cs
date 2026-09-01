using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSModelHotkeysResponse : VTSResponse<VTSModelHotkeysResponseData>;
public sealed class VTSModelHotkeysResponseData : VTSResponseData
{
    [JsonPropertyName("modelLoaded")] public required bool ModelLoaded { get; set; }
    [JsonPropertyName("modelName")] public required string? ModelName { get; set; }
    [JsonPropertyName("modelID")] public required string? ModelID { get; set; }
    [JsonPropertyName("availableHotkeys")] public required Hotkey[]? AvailableHotkeys { get; set; }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    AppendLine(b, prefix, ModelLoaded);
    //    AppendLine(b, prefix, ModelName);
    //    AppendLine(b, prefix, ModelID);
    //    AppendList(b, prefix, AvailableHotkeys, Hotkey.ToString).AppendLine();
    //    return base.ToString(b, prefix);
    //}

    public readonly struct Hotkey
    {
        [JsonPropertyName("name")] public required string? Name { get; init; }
        [JsonPropertyName("type")] public required string? Type { get; init; }
        [JsonPropertyName("description")] public required string? Description { get; init; }
        [JsonPropertyName("file")] public required string? File { get; init; }
        [JsonPropertyName("hotkeyID")] public required string? HotkeyID { get; init; }
        [JsonPropertyName("onScreenButtonID")] public required int OnScreenButtonID { get; init; }

        public override string ToString() => VTSPackets.ToLoggingString(this);
        //public override string? ToString() => ToString(VTSHelpers.DefaultPrefix);
        //public string? ToString(string prefix) => ToString(b: new(), prefix).ToString();
        //public static StringBuilder ToString(Hotkey hotkey, StringBuilder b, string prefix = VTSHelpers.DefaultPrefix) => hotkey.ToString(b, prefix);
        //public StringBuilder ToString(StringBuilder b, string prefix = VTSHelpers.DefaultPrefix)
        //{
        //    VTSHelpers.AppendLine(b, prefix, Name);
        //    VTSHelpers.AppendLine(b, prefix, Type);
        //    VTSHelpers.AppendLine(b, prefix, Description);
        //    VTSHelpers.AppendLine(b, prefix, File);
        //    VTSHelpers.AppendLine(b, prefix, HotkeyID);
        //    VTSHelpers.Append(b, prefix, OnScreenButtonID);
        //    return b;
        //}
    }
}