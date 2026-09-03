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

using System.Collections.ObjectModel;
using VoiceTrigger.Logging;

namespace VoiceTrigger.VTS;

public sealed class VTSService : IService
{
    public static readonly VTSService Instance = new();

    public ObservableCollection<VTSConnection> Connections = [];

    readonly Lock Lock = new();

    public void Initialize()
    {
        if (!VTSDiscoveryService.Instance.Active)
        {
            $"{this} {nameof(VTSDiscoveryService)} isn't active! Unless it's activated later, VTubeStudio plugin won't work!".Out(ConsoleColor.Red);
        }

        lock (Lock)
        {

        }
    }

    public void Terminate()
    {
        lock (Lock)
        {

        }
    }

    public override string ToString() => $"[{nameof(VTSService)}]";
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
