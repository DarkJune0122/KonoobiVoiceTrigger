using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;
using VTS.Core;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSHotkeyTriggerResponse : VTSResponse<VTSHotkeyTriggerResponseData>;
public sealed class VTSHotkeyTriggerResponseData : VTSResponseData
{
    [JsonPropertyName("hotkeyID")] public string? HotkeyID { get; set; }

    public override void Reset()
    {
        base.Reset();
        HotkeyID = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    AppendLine(b, prefix, HotkeyID);
    //    return base.ToString(b, prefix);
    //}
}