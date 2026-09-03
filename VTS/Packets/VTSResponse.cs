using System.Text.Json.Serialization;

namespace VoiceTrigger.VTS.Packets;

// Note: Maybe use pool TData and Rent/Return it on Json deserialization/disposal?
public abstract class VTSResponseTemplate : VTSPacket
{
    /// <summary>
    /// Whether this response indicates a successful response or not.
    /// </summary>
    public abstract bool Succeeded { get; }
}

public abstract class VTSResponse<TData> : VTSResponseTemplate where TData : VTSResponseData
{
    /// <summary>
    /// Whether it is a successful response.
    /// </summary>
    public sealed override bool Succeeded => Data is not null && Data.Succeeded;

    [JsonPropertyName("data")] public virtual TData? Data { get; set; }

    public override void Reset()
    {
        base.Reset();
        // We keep the reference to allow JsonSerializer to deserialize values directly into the existing reference.
        Data?.Reset();
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix).AppendLine();
    //    AppendData(b, prefix, Data);
    //    return b;
    //}
}

public class VTSResponse : VTSResponseTemplate
{
    /// <summary>
    /// Whether it is a successful response.
    /// </summary>
    /// <remarks>
    /// Lack of <see cref="Data"/> also counts as a success, since it means there is no ErrorIDs to report.
    /// </remarks>
    public sealed override bool Succeeded => Data is null || Data.Succeeded;

    [JsonPropertyName("data")] public virtual VTSResponseData? Data { get; set; }

    public override void Reset()
    {
        base.Reset();
        // We keep the reference to allow JsonSerializer to deserialize values directly into the existing reference.
        Data?.Reset();
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    base.ToString(b, prefix).AppendLine();
    //    AppendData(b, prefix, Data);
    //    return b;
    //}
}

public class VTSResponseData : VTSPacketData
{
    /// <summary>
    /// Checks if no errors have occurred during the request.
    /// </summary>
    public bool Succeeded => ErrorID == 0;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("errorID")] public long ErrorID { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("message")] public string? Message { get; set; }

    public override void Reset()
    {
        base.Reset();
        ErrorID = default;
        Message = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    AppendLine(b, prefix, ErrorID);
    //    Append(b, prefix, Message);
    //    return b;
    //}
}
