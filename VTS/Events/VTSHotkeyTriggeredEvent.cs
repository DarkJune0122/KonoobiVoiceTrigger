using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSHotkeyTriggeredEvent : VTSResponse<VTSHotkeyTriggeredEventData>;
public sealed class VTSHotkeyTriggeredEventData : VTSResponseData
{
    [JsonPropertyName("hotkeyID")] public required string? HotkeyID { get; init; }
    [JsonPropertyName("hotkeyName")] public required string? HotkeyName { get; init; }
    [JsonPropertyName("hotkeyAction")] public required string? HotkeyAction { get; init; }
    [JsonPropertyName("hotkeyFile")] public required string? HotkeyFile { get; init; }
    [JsonPropertyName("hotkeyTriggeredByAPI")] public required bool HotkeyTriggeredByAPI { get; init; }
    [JsonPropertyName("modelID")] public required string? ModelID { get; init; }
    [JsonPropertyName("modelName")] public required string? ModelName { get; init; }
    [JsonPropertyName("isLive2DItem")] public required bool IsLive2DItem { get; init; }

    public override StringBuilder ToString(StringBuilder b, string prefix = "")
    {
        base.ToString(b, prefix);
        AppendLine(b, prefix, HotkeyID);
        AppendLine(b, prefix, HotkeyName);
        AppendLine(b, prefix, HotkeyAction);
        AppendLine(b, prefix, HotkeyFile);
        AppendLine(b, prefix, HotkeyTriggeredByAPI);
        AppendLine(b, prefix, ModelID);
        AppendLine(b, prefix, ModelName);
        Append(b, prefix, IsLive2DItem);
        return b;
    }
}