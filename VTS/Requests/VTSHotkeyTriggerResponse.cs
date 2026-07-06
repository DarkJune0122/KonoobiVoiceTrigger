using System.Text;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSHotkeyTriggerResponse : VTSResponse<VTSHotkeyTriggerResponseData>;
public sealed class VTSHotkeyTriggerResponseData : VTSResponseData
{
    [JsonPropertyName("hotkeyID")] public required string? HotkeyID { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        AppendLine(b, prefix, HotkeyID);
        return base.ToString(b, prefix);
    }
}