using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSHotkeyTriggeredEvent : VTSResponse<VTSHotkeyTriggeredEventData>;
public sealed class VTSHotkeyTriggeredEventData : VTSResponseData
{
    [JsonPropertyName("hotkeyID")] public required string? HotkeyID { get; set; }
    [JsonPropertyName("hotkeyName")] public required string? HotkeyName { get; set; }
    [JsonPropertyName("hotkeyAction")] public required string? HotkeyAction { get; set; }
    [JsonPropertyName("hotkeyFile")] public required string? HotkeyFile { get; set; }
    [JsonPropertyName("hotkeyTriggeredByAPI")] public required bool HotkeyTriggeredByAPI { get; set; }
    [JsonPropertyName("modelID")] public required string? ModelID { get; set; }
    [JsonPropertyName("modelName")] public required string? ModelName { get; set; }
    [JsonPropertyName("isLive2DItem")] public required bool IsLive2DItem { get; set; }

    public override void Reset()
    {
        base.Reset();
        HotkeyID = default;
        HotkeyName = default;
        HotkeyAction = default;
        HotkeyFile = default;
        HotkeyTriggeredByAPI = default;
        ModelID = default;
        ModelName = default;
        IsLive2DItem = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = "")
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, HotkeyID);
    //    AppendLine(b, prefix, HotkeyName);
    //    AppendLine(b, prefix, HotkeyAction);
    //    AppendLine(b, prefix, HotkeyFile);
    //    AppendLine(b, prefix, HotkeyTriggeredByAPI);
    //    AppendLine(b, prefix, ModelID);
    //    AppendLine(b, prefix, ModelName);
    //    Append(b, prefix, IsLive2DItem);
    //    return b;
    //}
}