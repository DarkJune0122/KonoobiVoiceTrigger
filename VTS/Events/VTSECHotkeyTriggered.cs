using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Events;

public sealed class VTSECHotkeyTriggered : VTSEventConfig
{
    [JsonPropertyName("onlyForAction")] public required string? OnlyForAction { get; set; }
    [JsonPropertyName("ignoreHotkeysTriggeredByAPI")] public required string? IgnoreHotkeysTriggeredByAPI { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = "")
    {
        AppendLine(b, prefix, OnlyForAction);
        Append(b, prefix, IgnoreHotkeysTriggeredByAPI);
        return b;
    }
}