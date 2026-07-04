using System.Text;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public class VTSResponse : VTSResponse<object?>;
public class VTSResponse<TData> : VTSPacket
{
    [JsonPropertyName("data")] public virtual TData? Data { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix, bool newLine = DefaultNewLine)
    {
        base.ToString(b, prefix, false);
        AppendLine(b, prefix, Data);
        return AppendData(b, prefix, Data, newLine);
    }
}
