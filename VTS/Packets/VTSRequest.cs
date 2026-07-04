using System.Text;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSRequest : VTSRequest<object?>;
public abstract class VTSRequest<TData> : VTSRequestPacket
{
    [JsonPropertyName("data")] public required TData? Data { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix, bool newLine = DefaultNewLine)
    {
        base.ToString(b, prefix, false);
        AppendLine(b, prefix, Data);
        return AppendData(b, prefix, Data, newLine);
    }
}
