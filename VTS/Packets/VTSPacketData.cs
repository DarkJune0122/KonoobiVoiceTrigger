using System.Text.Json;

namespace VoiceTrigger.VTS.Packets;

public abstract class VTSPacketData
{
    public override string ToString() => JsonSerializer.Serialize(this, GetType(), VTSPackets.JsonOptions);
}
