using System.Text;
using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

// Note: Maybe use pool TData and Rent/Return it on Json deserialization/disposal?
public abstract class VTSResponseTemplate : VTSPacket;
public abstract class VTSResponse<TData> : VTSResponseTemplate where TData : VTSResponseData
{
    // Lack of data also counts as a success, since data will only exist if there is no 
    public bool Succeeded => Data is null || Data.Succeeded;

    [JsonPropertyName("data")] public virtual TData? Data { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        base.ToString(b, prefix).AppendLine();
        AppendData(b, prefix, Data);
        return b;
    }
}

public class VTSResponse : VTSResponse<VTSResponseData>;
public class VTSResponseData : VTSPacketData
{
    public bool Succeeded => ErrorID == 0;

    [JsonPropertyName("errorID")] public long ErrorID { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    {
        AppendLine(b, prefix, ErrorID);
        Append(b, prefix, Message);
        return b;
    }
}
