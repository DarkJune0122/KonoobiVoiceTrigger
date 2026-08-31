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
