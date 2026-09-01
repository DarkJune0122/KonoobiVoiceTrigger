using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoiceTrigger.Logging;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS;

public static class VTSPackets
{
    public const string APINameJsonPropertyName = "apiName";
    public const string APIVersionJsonPropertyName = "apiVersion";
    public const string RequestIDJsonPropertyName = "requestID";
    public const string MessageTypeJsonPropertyName = "messageType";
    /// <summary>
    /// Dummy successful request result.
    /// </summary>
    public static readonly VTSRequest DummyRequest = new();
    /// <summary>
    /// Dummy successful response result.
    /// </summary>
    public static readonly VTSResponse DummyResponse = new() { Data = null };
    /// <summary>
    /// Options to use for JSON serialization in <see cref="JsonSerializer"/>.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        TypeInfoResolver = VTSJsonTypeInfoResolver.Instance,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
    };
    /// <summary>
    /// Options to use for JSON serialization in <see cref="JsonSerializer"/> specifically for logging.
    /// </summary>
    public static readonly JsonSerializerOptions JsonLoggingOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        TypeInfoResolver = VTSJsonTypeInfoResolver.Instance,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
    };
    /// <summary>
    /// Options to use for Json serialization in <see cref="JsonDocument"/>.
    /// </summary>
    public static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 0, // Maximum.
    };

    [return: NotNullIfNotNull(nameof(obj))]
    public static string? ToLoggingString<T>(this T? obj, Type? type = null)
    {
        if (obj is null) return null;
        type ??= typeof(T);

        try { return JsonSerializer.Serialize(obj, type, JsonLoggingOptions); }
        catch (Exception ex) { ex.Out($"Cannot serialize ({typeof(T)})\n"); return string.Empty; }
    }
}
