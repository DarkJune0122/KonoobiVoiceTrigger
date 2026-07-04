using System.Text;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSResponsePacket : VTSPacket
{
    [JsonPropertyName("timestamp")] public virtual long Timestamp { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix, bool newLine = DefaultNewLine)
    {
        base.ToString(b, prefix, false);
        Append(b, prefix, Timestamp, newLine);
        return b;
    }
}
