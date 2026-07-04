namespace VoiceTrigger.VTS.Packets;

public abstract class VTSRequestPacket : VTSPacket
{
    public override string? APIName { get; set; } = DefaultAPIName;
    public override string? APIVersion { get; set; } = DefaultAPIVersion;
}
