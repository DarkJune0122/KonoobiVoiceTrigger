// VTSService:
// 1. Warn if initialized without VTSDiscoveryService being active as well.
// 2. Track new VTSEndPoints in VTSDiscoveryService.
//    - Automatically initialize VTSConnections using those end-points.
// 3. Track VTSEndPoint invalidation / VTSEndPoint removal from VTSDiscoveryService.
//    - Terminate VTSConnections using those VTSEndPoints.
//    - Do it unless it's a responsibility of a VTSConnection to do so.
// 4. Notify about status changes (Active/Inactive).
// 5. Handle all possible issues related to networking, log them, and make sure they never cause a crash.
// 6. Additionally, later verify that App.cs doesn't cause any issues with its usage Pipes and Mutexes outside of a try block.

// VTSConnection:
// 1. Provide API to send requests.
// 2. Provide API to receive request responses.
// 3. Timeout active requests if associated VTSEndPoint closes.
// 4. Provide UI-safe Status and Authenticated properties.
// 5. Provide wrapper around it as a VTubeStudio, when using VTSService with only 1 instance allowed to exist at a time.
// 6. Communicate using pooled network packets.
//    - When applicable, use pre-serialized json payloads for immutable/pre-constructed packets.
//    - Potentially allow to specify how a packet instance should be created (or retrieved from a static cache)
// 7. Communicate using mutable(!) VTSPlugin data class.
//    - (Optionally) Close and re-authenticate the connection if VTSPlugin data changes.
// 8. Communicate using token from a token manager, attached to each VTSPlugin (depending on implementation, might be the same instance(?)).

using CommunityToolkit.Mvvm.ComponentModel;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VoiceTrigger.Logging;
using VoiceTrigger.VTS.Packets;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

/// <summary>
/// Class communicating with an VTube Studio instance.
/// </summary>
/// <remarks>
/// <para>
/// v1.0:
/// To support StackOnly Json Deserialization in the future, there has to be a Request method returning raw bytes.
/// This is so we can use it as ReadOnlySpan in the deserializer.
/// </para>
/// </remarks>
public partial class VTSConnection(VTSEndPoint ep) : ObservableObject
{
    // TODO: Add EndPoint broken callback. Delist VTSConnection when this happens. Notify users about this as well.
    enum ConnectResult : byte
    {
        Success,
        AlreadyActivated,
        Failed,
    }

    enum DisconnectResult : byte
    {
        Success,
        AlreadyDisconnected,
        Failed,
    }

    static long InstanceCounter;
    public long ID { get; } = NextInstanceID();
    public VTSEndPoint EndPoint { get; } = ep;

    [ObservableProperty] public partial VTSPlugin? Plugin { get; protected set; }
    [ObservableProperty] public partial VTSStatus Status { get; protected set; }

    protected readonly Lock Lock = new();
    protected VTSSocket? Socket = new();

    public void Reconnect()
    {
        TryDisconnect();
        TryConnect();
    }

    public bool TryConnect()
    {
        ConnectResult startResult = ConnectInternal();
        switch (startResult)
        {
            case ConnectResult.Failed:
                $"{this} Failed to connect!".Out(ConsoleColor.Red);
                return false;

            case ConnectResult.AlreadyActivated:
            case ConnectResult.Success: return true;
            default: throw new SwitchExpressionException(startResult);
        }
    }

    public void Connect()
    {
        ConnectResult result = ConnectInternal();
        switch (result)
        {
            case ConnectResult.Failed:
                $"{this} Failed to connect!".Out(ConsoleColor.Red);
                break;

            case ConnectResult.AlreadyActivated: // Replace with exception?
                $"{this} Already connecting/connected.".Out(ConsoleColor.Yellow);
                break;

            case ConnectResult.Success: break;
            default: throw new SwitchExpressionException(result);
        }
    }

    public bool TryDisconnect()
    {
        DisconnectResult result = DisconnectInternal();
        switch (result)
        {
            case DisconnectResult.Failed:
                $"{this} Failed to disconnect!".Out(ConsoleColor.Red);
                return false;

            case DisconnectResult.AlreadyDisconnected:
            case DisconnectResult.Success: return true;
            default: throw new SwitchExpressionException(result);
        }
    }

    public void Disconnect()
    {
        DisconnectResult result = DisconnectInternal();
        switch (result)
        {
            case DisconnectResult.Failed:
                $"{this} Failed to disconnect!".Out(ConsoleColor.Red);
                break;

            case DisconnectResult.AlreadyDisconnected: // Replace with exception?
                $"{this} Already disconnected.".Out(ConsoleColor.Yellow);
                break;

            case DisconnectResult.Success: break;
            default: throw new SwitchExpressionException(result);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long NextInstanceID() => Interlocked.Increment(ref InstanceCounter);
    ConnectResult ConnectInternal()
    {
        Lock.Enter();
        try
        {
            if (Socket is not null)
            {
                return ConnectResult.AlreadyActivated;
            }
            if (!EndPoint.Alive)
            {
                $"{this} VTube Studio #{EndPoint.InstanceID} has already quit.".Out(ConsoleColor.Yellow);
                return ConnectResult.Failed;
            }

            // TODO: Verify it works, and finish the connection sequence.
            Uri uri = new($"ws://localhost:{EndPoint.Port}");
            VTSSocket socket = new();
            Socket = socket;
            Status = VTSStatus.Connecting;
            ConnectionSequence(socket, uri);
            return ConnectResult.Success;
        }
        catch (Exception ex) // No handler for cancellation, as it is expected to never happen within a lock scope.
        {
            ex.Out(this);
            Socket = null;
            try { Status = VTSStatus.Offline; }
            catch (Exception ex2) { ex2.Out($"{this} Failed to reset the status!\n"); }
            return ConnectResult.Failed;
        }
        finally { Lock.Exit(); }
    }

    async void ConnectionSequence(VTSSocket socket, Uri uri)
    {
        // Expected: Initialization exceptions propagate back to called with a try-catch exception handling.
        var initialization = socket.ConnectAsync(uri);
        var token = socket.Token;
        try
        {
            await initialization;
            // TODO: Finish the connection sequence.
        }
        catch (OperationCanceledException) { /* No cancellation action */ }
        catch (Exception ex) { ex.Out(this); }
        finally
        {
            lock (Lock)
            {
                if (Socket == socket)
                {
                    Socket = null;
                    try { Status = VTSStatus.Offline; }
                    catch (Exception ex) { ex.Out($"{this} Failed to reset the status after socket quitting!\n"); }
                }
            }
        }
    }

    DisconnectResult DisconnectInternal()
    {
        Lock.Enter();
        try
        {
            if (Socket is null)
            {
                return DisconnectResult.AlreadyDisconnected;
            }

            // TODO: Finish.

            return DisconnectResult.Success;
        }
        catch (Exception ex) { ex.Out(this); return DisconnectResult.Failed; }
        finally { Lock.Exit(); }
    }

    public async ValueTask<TResponse> Request<TResponse>(VTSRequestTemplate request) where TResponse : VTSResponseTemplate
    {
        // Optional: If failed - return static template by default.
        //  But do so only if we can confirm that a failed Respose will ALWAYS has the same ErrorID and a message.
        // As it violates current VTube Studio API, which allows for new ErrorIDs to be added - we will not implement this feature.
        Lock.Enter();
        try
        {
            if (Socket is null)
                throw new NotImplementedException();

            // TODO: Check for type to have an immutable attibute, and use a cached value instead.
            //  Cached value can be stored both using a lookup map (indexsing base on GetType()).
            //  As well as using generics in this method.
            //  Both require using concurrent dictionary or relying on syncronized static .ctor initialization.
            await RequestRaw(JsonSerializer.Serialize(request, request.GetType(), VTSPackets.JsonOptions)); // Example implementation. TODO: Improve.
        }
        catch (Exception ex) { ex.Out(); throw new NotImplementedException(); } // TODO: Return a standard failed response here.
        finally { Lock.Exit(); }
        throw new NotImplementedException();
    }

    /// <summary>
    /// Same as <see cref="Request{TResponse}(VTSRequestTemplate)"/>,
    /// but after completion (or on exception) releases <paramref name="request"/> back to pool using <see cref="VTSPackets.Return{T}(T)"/>
    /// </summary>
    /// <remarks>
    /// <see cref="JsonSerializer"/> will use <typeparamref name="TRequest"/> as a type,
    /// instead of <see cref="object.GetType"/>, allowing you to specify how you want your request to be serialized.
    /// </remarks>
    public async ValueTask<TResponse> RequestWithReturn<TResponse, TRequest>(TRequest request)
        where TResponse : VTSResponseTemplate where TRequest : VTSRequestTemplate
    {
        Lock.Enter();
        try
        {
            if (Socket is null)
                throw new NotImplementedException();

            // TODO: Check for type to have an immutable attibute, and use a cached value instead.
            //  Cached value can be stored both using a lookup map (indexsing base on GetType()).
            //  As well as using generics in this method.
            //  Both require using concurrent dictionary or relying on syncronized static .ctor initialization.
            await RequestRaw(JsonSerializer.Serialize(request, VTSPackets.JsonOptions)); // Example implementation. TODO: Improve.
        }
        catch (Exception ex) { ex.Out(); throw new NotImplementedException(); } // TODO: Return a standard failed response here.
        finally { Lock.Exit(); VTSPackets.Return(request); }
        throw new NotImplementedException();
    }

    /// <summary>
    /// Sends raw <paramref name="json"/> data to the VTube Studio.
    /// Anything VTube Studio will return will be returned after this task completes.
    /// </summary>
    /// TODO: Add an internal, more optimized version for usage in all request methods we provide here.
    ///  Make sure to reduce the amount of state machines we spawn its possible minimum - as little async methods as possible please.
    /// TODO: Split into Core (protected) and public methods, and make Core one not use locking. Add this to the remarks of Core method.
    public ValueTask<string> RequestRaw(string json)
    {
        // TODO: Add state checks as well.
        throw new NotImplementedException();
    }

    public override string ToString() => $"[{nameof(VTSConnection)} #{ID}]";
}

/// <summary>
/// Json result of the request, normally in UTF8 format.
/// </summary>
/// <remarks>
/// 
/// </remarks>
//public readonly ref struct JsonResult
//{
//    public readonly bool Success;
//    public readonly ReadOnlySpan<byte> RequestID;
//    public readonly ReadOnlySpan<byte> DataJson;
//}

public readonly ref struct Packet(
    ReadOnlySpan<char> apiName, ReadOnlySpan<char> apiVersion,
    ReadOnlySpan<char> messageType, ReadOnlySpan<char> requestID, ReadOnlySpan<char> data)
{
    public readonly ReadOnlySpan<char> APIName = apiName;
    public readonly ReadOnlySpan<char> APIVersion = apiVersion;
    public readonly ReadOnlySpan<char> MessageType = messageType;
    public readonly ReadOnlySpan<char> RequestID = requestID;
    public readonly ReadOnlySpan<char> Data = data;

    public bool TryDeserializeJson(ReadOnlySpan<byte> utf8Json, out Packet packet)
    {
        Utf8JsonReader reader = new(utf8Json, VTSPackets.ReaderOptions);
        char[] buffer = ArrayPool<char>.Shared.Rent(VTSPackets.Encoding.GetMaxCharCount(utf8Json.Length));
        Packet result = new();
        try
        {
            // TODO: Read all known json payload header properties.
            // Note: Deserialize data by simply providing it as raw byte ReadOnlySpan or ReadOnlyMemory instead.
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        throw new NotImplementedException();

                    case JsonTokenType.String:
                        throw new NotImplementedException();

                    case JsonTokenType.StartObject:
                        throw new NotImplementedException();

                    case JsonTokenType.EndObject:
                        throw new NotImplementedException();

                    // Non applicable for this packet type.
                    case JsonTokenType.StartArray:
                    case JsonTokenType.EndArray:
                    case JsonTokenType.None:
                    case JsonTokenType.Number:
                    case JsonTokenType.Comment:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                    case JsonTokenType.Null:
                        break;

                    // Default case has to throw to better optimize jump-table branch prediction.
                    default: throw new SwitchExpressionException(reader.TokenType);
                }
            }
        }
        catch (Exception ex)
        {
            ex.Out($"Failed to deserialize {nameof(Packet)}!");
            if (Roaming.LogNetworkPackets)
            {
                try
                {
                    $"Failed packet: {VTSPackets.Encoding.GetString(utf8Json)}".Out(ConsoleColor.Red);
                }
                catch (Exception ex2) { ex2.Out($"Cannot log failed packet - error while decoding."); }
            }
        }
        finally { ArrayPool<char>.Shared.Return(buffer); }
    }
}

/// <summary>
/// Useful methods for requesting specific things with more readable API
/// </summary>
/// <remarks>
/// Not finished - a lot of possible requests are still missing!
/// </remarks>
/// TODO: Implement using IVTSConnection/IVTSInstance interface instead, for better compatibility with custom implementations.
public static class VTSConnectionExtensions
{
    public static ValueTask<VTSAPIStateResponse> RequestAPIState(this VTSConnection vts)
    {
        return vts.Request<VTSAPIStateResponse>(VTSAPIStateRequest.Instance);
    }

    public static void RequestAIPState(this VTSConnection vts, Action<VTSAPIStateResponse> handler)
    {

    }

    public static void RequestAIPState(this VTSConnection vts, Action<VTSAPIStateResponse> onSuccess, Action onFailed)
    {

    }

    public static ValueTask<VTSExpressionStateResponse> RequestExpressionState(this VTSConnection vts, string? expressionFile, bool details = false)
    {
        VTSExpressionStateRequest request = VTSPackets.Rent(Construct);
        request.Data.Details = details;
        request.Data.ExpressionFile = expressionFile;
        return vts.RequestWithReturn<VTSExpressionStateResponse, VTSExpressionStateRequest>(request);
        static VTSExpressionStateRequest Construct() => new()
        {
            Data = new() { Details = false, ExpressionFile = null },
        };
    }

    public static ValueTask<VTSModelHotkeysResponse> RequestModelHotkeys(this VTSConnection vts, string? modelID, string? live2DItemFileName = null)
    {
        VTSModelHotkeysRequest request = VTSPackets.Rent(Construct);
        request.Data.ModelID = modelID;
        request.Data.Live2DItemFileName = live2DItemFileName;
        return vts.RequestWithReturn<VTSModelHotkeysResponse, VTSModelHotkeysRequest>(request);
        static VTSModelHotkeysRequest Construct() => new()
        {
            Data = new() { ModelID = null, Live2DItemFileName = null, }
        };
    }
}

//using CommunityToolkit.Mvvm.ComponentModel;
//using System.Diagnostics.CodeAnalysis;
//using VTS.Core;

//namespace VoiceTrigger.VTS;

//public sealed partial class VTSService : ObservableObject
//{
//    public static readonly VTSService Instance = new();
//    // Trying high values first.
//    // I want to react to events rather than relying on UPS.
//    // Impulse systems rule! Just need to make sure there will be no lag spikes.
//    const int UpdateIntervalMs = 1000;
//    const int RestartIntervalMs = 2500;

//    [ObservableProperty] public partial VTSStatus Status { get; private set; }
//    [ObservableProperty] public partial bool Authenticated { get; private set; }

//    [MemberNotNullWhen(true, nameof(Identity))]
//    bool IsInitialized { get; set; }
//    CancellationTokenSource? Identity { get; set; }

//    readonly Lock Lock = new();

//    public void Initialize()
//    {
//        lock (Lock)
//        {
//            if (!IsInitialized)
//            {
//                try
//                {
//                    Identity = new();
//                    IWebSocket socket = new WebSocketImpl(CustomVTSLogger.Instance);
//                    IJsonUtility json = new NewtonsoftJsonUtilityImpl();
//                    ITokenStorage storage = new TokenStorageImpl("");
//                    IVTSPlugin plugin = new CoreVTSPlugin(socket, json, storage, CustomVTSLogger.Instance,
//                                                          UpdateIntervalMs, "Voice Trigger", "Sandcorp, SoG", VTSIconProvider.IconBase64);
//                    ManagerWorker(plugin, Identity);
//                    IsInitialized = true;
//                }
//                catch (Exception ex) { ex.Out($"{this} Failed to gracefully initialize the service!\n"); }
//            }
//        }
//    }

//    public void Terminate()
//    {
//        lock (Lock)
//        {
//            if (IsInitialized)
//            {
//                IsInitialized = false;
//                try
//                {

//                }
//                catch (Exception ex) { ex.Out($"{this} Failed to gracefully terminate the service!\n"); }
//            }
//        }
//    }

//    async void ManagerWorker(IVTSPlugin plugin, CancellationTokenSource identity)
//    {
//        await Task.Yield();
//        $"{this} Starting VTubeStudio plugin worker.".Out();
//        while (!identity.IsCancellationRequested)
//        {
//            try
//            {
//                await Worker(plugin, identity);
//                $"{this} VTubeStudio plugin worker exited unexpectedly! Plugin will restart shortly".Out(ConsoleColor.Yellow);
//            }
//            catch (OperationCanceledException) { break; }
//            catch (Exception ex) { ex.Out($"{this} Exception in a VTubeStudio plugin worker. Plugin will restart shortly.\n"); }

//            if (identity.IsCancellationRequested) break;
//            SetAuthenticated(identity, false);
//            await Task.Delay(RestartIntervalMs);
//            $"{this} Restarting plugin worker.".Out();
//        }
//        SetAuthenticated(identity, false);
//    }

//    async Task Worker(IVTSPlugin plugin, CancellationTokenSource identity)
//    {
//        var token = identity.Token;

//        $"{this} Connecting to VTubeStudio.".Out(ConsoleColor.Gray);
//        await plugin.InitializeAsync(static () => $"{Instance} Plugin disconnected.".Out(ConsoleColor.Gray));
//        token.ThrowIfCancellationRequested();
//        $"{this} Plugin connected!".Out(ConsoleColor.Gray);

//        var api = await plugin.GetAPIState();
//        token.ThrowIfCancellationRequested();
//        $"{this} Using VTubeStudio: {api.apiVersion}".Out(ConsoleColor.Gray);

//        SetAuthenticated(identity, true);
//    }

//    void SetAuthenticated(CancellationTokenSource identity, bool authenticated)
//    {

//    }

//    public override string ToString() => $"[{nameof(VTSService)}]";
//}
