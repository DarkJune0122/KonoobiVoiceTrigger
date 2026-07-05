using System.Text;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public class VTSResponse : VTSResponse<object?>;
public class VTSResponse<TData> : VTSResponsePacket
{
    [JsonPropertyName("data")] public virtual TData? Data { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        base.ToString(b, prefix).AppendLine();
        AppendData(b, prefix, Data);
        return b;
    }
}
