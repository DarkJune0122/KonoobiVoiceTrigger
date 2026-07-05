using System.Text;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSRequest : VTSRequest<object?>;
public abstract class VTSRequest<TData> : VTSRequestPacket
{
    [JsonPropertyName("data")] public required TData? Data { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        base.ToString(b, prefix).AppendLine();
        AppendData(b, prefix, Data);
        return b;
    }
}
