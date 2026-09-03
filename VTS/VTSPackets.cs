using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoiceTrigger.Logging;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS;

/// <summary>
/// TODO: Move to VTSHelpers, to remove ambiduity with <see cref="VTSPacket"/> and <see cref="VTSPacketData"/>.
/// Note: Pooling can be separated in its own class for clarity, like VTSPacketPool.
/// </summary>
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

    // Pooling:
    static class Pool<T> where T : VTSPacketData
    {
        const int DefaultCapacity = 16;
        public static int Capacity
        {
            get
            {
                lock (Lock) return field;
            }
            set
            {
                value = Math.Max(0, value);
                lock (Lock)
                {
                    if (field != value)
                    {
                        // Truncates the stack.
                        int count = Stack.Count;
                        if (count > value)
                        {
                            do
                            {
                                Stack.Pop();
                                count--; // Avoids re-fetching via Count property.
                            }
                            while (count > value);
                            Stack.TrimExcess(value);
                        }
                        else if (count > value)
                        {
                            Stack.EnsureCapacity(value);
                        }
                        field = value;
                    }
                }
            }
        } = DefaultCapacity;

        static readonly Lock Lock = new();
        static readonly Stack<T> Stack = new(DefaultCapacity);
        public static Y Rent<Y>() where Y : T, new()
        {
            lock (Lock)
            {
                if (Stack.TryPop(out var res))
                    return (Y)res;
            }

            return new();
        }

        public static T Rent(Func<T> ctor)
        {
            lock (Lock)
            {
                if (Stack.TryPop(out var res))
                    return res;
            }

            return ctor();
        }

        public static void Return(T inst)
        {
            // Note: most memory performant solution will be to reset values after checking if stack has space for the item.
            //  This way, GC will be able to collect the reference with all the fields, without us needing to do extra work.
            //  But doing so requires us resetting the item in the lock scope, which will consume shared resources.
            //  No benchmarking was done on this method, so decision whether to leave it out of the score was made arbtrarily,
            inst.Reset();
            lock (Lock)
            {
                if (Stack.Count < Capacity)
                    Stack.Push(inst);
            }
        }
    }

    public static int GetPoolCapacity<T>() where T : VTSPacketData => Pool<T>.Capacity;
    public static void SetPoolCapacity<T>(int capacity) where T : VTSPacketData, new() => Pool<T>.Capacity = capacity;
    public static T Rent<T>(Func<T> ctor) where T : VTSPacketData => Pool<T>.Rent(ctor);
    public static T Rent<T>() where T : VTSPacketData, new() => Pool<T>.Rent<T>();
    public static void Return<T>(T packet) where T : VTSPacketData => Pool<T>.Return(packet);
}
