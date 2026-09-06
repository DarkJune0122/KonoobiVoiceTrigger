using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

// Note: Maybe use pool TData and Rent/Return it on Json deserialization/disposal?
public class VTSRequestTemplate : VTSPacket
{
    // For simplicity, those two fields are filled-in automatically.
    protected const string DefaultAPIName = "VTubeStudioPublicAPI";
    protected const string DefaultAPIVersion = "1.0";

    // MessageType and RequestID is are filled in by entity constructing the request.
    [JsonPropertyName("apiName")] public override string? APIName { get; set; } = DefaultAPIName;
    [JsonPropertyName("apiVersion")] public override string? APIVersion { get; set; } = DefaultAPIVersion;
}

public abstract class VTSRequest<TData> : VTSRequestTemplate where TData : VTSRequestData
{
    [JsonPropertyName("data")] public virtual required TData Data { get; set; }

    public override void Reset()
    {
        base.Reset();
        Data.Reset(); // Allows data to be reused by JsonSerializer and user code.
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix).AppendLine();
    //    AppendData(b, prefix, Data);
    //    return b;
    //}
}

/// <summary>
/// Request without a data entry.
/// </summary>
/// <remarks>
/// Non-abstract only to be able to create <see cref="VTSPackets.DummyRequest"/>.
/// </remarks>
public class VTSRequest : VTSRequestTemplate;
public abstract class VTSRequestData : VTSPacketData
{
    //public override StringBuilder ToString(StringBuilder b, string prefix = "") => b;
}
