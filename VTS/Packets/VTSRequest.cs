using System.Text;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

// Note: Maybe use pool TData and Rent/Return it on Json deserialization/disposal?
public class VTSRequestTemplate : VTSPacket
{
    [JsonPropertyName("apiName")] public override string? APIName { get; set; } = DefaultAPIName;
    [JsonPropertyName("apiVersion")] public override string? APIVersion { get; set; } = DefaultAPIVersion;
}

public abstract class VTSRequest<TData> : VTSRequestTemplate where TData : VTSRequestData
{
    [JsonPropertyName("data")] public virtual required TData? Data { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        base.ToString(b, prefix).AppendLine();
        AppendData(b, prefix, Data);
        return b;
    }
}

/// <summary>
/// Request without a data entry.
/// </summary>
public class VTSRequest : VTSRequestTemplate;
public class VTSRequestData : VTSPacketData
{
    public override StringBuilder ToString(StringBuilder b, string prefix = "") => b;
}
