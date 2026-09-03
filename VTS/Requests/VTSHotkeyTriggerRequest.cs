using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSHotkeyTriggerRequest : VTSRequest<VTSHotkeyTriggerRequestData>
{
    [JsonPropertyName("messageType")] public override string? MessageType { get; set; } = "HotkeyTriggerRequest";
}
public sealed class VTSHotkeyTriggerRequestData : VTSRequestData
{
    [JsonPropertyName("hotkeyID")] public required string? HotkeyID { get; set; }
    [JsonPropertyName("itemInstanceID")] public required string? ItemInstanceID { get; set; }

    public override void Reset()
    {
        base.Reset();
        HotkeyID = default;
        ItemInstanceID = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix);
    //    AppendLine(b, prefix, HotkeyID);
    //    Append(b, prefix, ItemInstanceID);
    //    return b;
    //}
}