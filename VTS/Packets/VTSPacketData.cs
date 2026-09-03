namespace VoiceTrigger.VTS.Packets;

public abstract class VTSPacketData
{
    /// <inheritdoc/>
    public override string ToString() => VTSPackets.ToLoggingString(this, GetType());
    /// <summary>
    /// Resets all data about the packet.
    /// </summary>
    /// <remarks>
    /// Used for pooling in <see cref="VTSPackets.Return{T}(T)"/>
    /// </remarks>
    public virtual void Reset() { }
}
