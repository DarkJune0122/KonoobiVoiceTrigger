namespace VoiceTrigger.VTS.Packets;

public abstract class VTSPacketData
{
    public override string ToString() => VTSPackets.ToLoggingString(this);
}
