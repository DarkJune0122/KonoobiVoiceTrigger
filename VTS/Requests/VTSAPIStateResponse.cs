using StackOnlyJsonParser;
using System.Text.Json.Serialization;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS.Requests;

public sealed class VTSAPIStateResponse : VTSResponse<VTSAPIStateResposeData>;
public sealed class VTSAPIStateResposeData : VTSResponseData
{
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("vTubeStudioVersion")] public string? VTubeStudioVersion { get; set; }
    [JsonPropertyName("currentSessionAuthenticated")] public bool CurrentSessionAuthenticated { get; set; }

    public override void Reset()
    {
        base.Reset();
        Active = default;
        VTubeStudioVersion = default;
        CurrentSessionAuthenticated = default;
    }

    //public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix)
    //{
    //    AppendLine(b, prefix, Active);
    //    AppendLine(b, prefix, VTubeStudioVersion);
    //    AppendLine(b, prefix, CurrentSessionAuthenticated);
    //    return base.ToString(b, prefix);
    //}
}

internal interface IVTSResponsePacket
{
    string? APIName { get; }
    string? APIVersion { get; }
    string? MessageType { get; }
    string? RequestID { get; }
}

internal interface IVTSResponsePacketData
{
    /// <summary>
    /// Whether response is successful.
    /// Unesuccessful responses have their <see cref="ErrorID"/> and <see cref="Message"/> properies initialized.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pre v1.0:
    /// Uses two checks chained together: <see cref="ErrorID"/> == 0 and <see cref="Message"/> == <see langword="null"/>.
    /// The latter is needed because <see cref="ErrorID"/> can return <see cref="ErrorID.InternalServerError"/>, which is also 0.
    /// This idea relies on <see cref="ErrorID.InternalServerError"/> always returning some kind of message though.
    /// In cases when <see cref="ErrorID.InternalServerError"/> returns <see langword="null"/> as a message,
    /// this will falsely mark packet response as successful and therefore - valid.
    /// </para>
    /// <para>
    /// v1.0: Json packets can be pre-fetched - Any responses with APIErrors can be explicitly marked as failed.
    /// </para>
    /// </remarks>
    bool Succeeded { get; }
    /// <summary>
    /// ID of the error.
    /// </summary>
    /// <remarks>
    /// If <see cref="Succeeded"/> is <see langword="true"/> - value of this Property is undefined.
    /// </remarks>
    ErrorID ErrorID { get; }
    /// <summary>
    /// Message for the current <see cref="ErrorID"/>
    /// </summary>
    /// <remarks>
    /// If <see cref="Succeeded"/> is <see langword="true"/> - value of this Property is undefined.
    /// </remarks>
    string? Message { get; }
}

[StackOnlyJsonType]
internal readonly ref partial struct VTSStackAPIStateResponse : IVTSResponsePacket
{
    [JsonPropertyName(VTSPackets.APINameJsonPropertyName)] public string? APIName { get; }
    [JsonPropertyName(VTSPackets.APIVersionJsonPropertyName)] public string? APIVersion { get; }
    [JsonPropertyName(VTSPackets.MessageTypeJsonPropertyName)] public string? MessageType { get; }
    [JsonPropertyName(VTSPackets.RequestIDJsonPropertyName)] public string? RequestID { get; }
    [JsonPropertyName(VTSPackets.DataJsonPropertyName)] public ResponseData Data { get; }
    public VTSStackAPIStateResponse(ErrorID errorID, string? message = null) => Data = new(errorID, message);
    public VTSStackAPIStateResponse(ResponseData data,
        string? apiName = null, string? apiVersion = null, string? messageType = null, string? requestID = null)
    {
        APIName = apiName;
        APIVersion
        Data = data;
    }

    public override string ToString()
    {
        throw new NotImplementedException("Source generator for serializing ref structs is not implemented yet.");
    }

    [StackOnlyJsonType]
    public readonly ref partial struct ResponseData(ErrorID errorID, string? message = null) : IVTSResponsePacketData
    {
        // Error handling header.
        [JsonIgnore] public bool Succeeded => ErrorID == default && Message is null;
        [StackOnlyJsonField(VTSPackets.ErrorIDJsonPropertyName)] public ErrorID ErrorID { get; } = errorID;
        [StackOnlyJsonField(VTSPackets.MessageJsonPropertyName)] public string? Message { get; } = message;

        // Content of the response.
        [StackOnlyJsonField("active")] public bool Active { get; }
        [StackOnlyJsonField("vTubeStudioVersion")] public StackOnlyJsonString VTubeStudioVersion { get; }
        [StackOnlyJsonField("currentSessionAuthenticated")] public bool CurrentSessionAuthenticated { get; }
    }
}