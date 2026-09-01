using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSResponsePacket : VTSPacket
{
    [JsonPropertyName("timestamp")] public virtual long Timestamp { get; set; }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix).AppendLine();
    //    Append(b, prefix, Timestamp);
    //    return b;
    //}
}
