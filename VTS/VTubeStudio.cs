using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using VoiceTrigger.VTS.Packets;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

public readonly struct VTSRequestResult
{
    // This is prefered, since it allows to not write "<T>" on each usage..
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VTSRequestResult<T> FromResult<T>(T? data) where T : VTSResponseTemplate => VTSRequestResult<T>.FromResult(data);
}

// TODO: Also return a reason of failure/success.
public readonly struct VTSRequestResult<T> where T : VTSResponseTemplate
{
    public static readonly VTSRequestResult<T> Failed = FromResult(null);
    public static VTSRequestResult<T> FromResult(T? response) => new(response);
    private VTSRequestResult(T? response)
    {
        Success = response is not null;
        Response = response;
    }

    [MemberNotNullWhen(true, nameof(Response))]
    public bool Success { get; init; }
    public T? Response { get; init; }
    public bool ResolveSuccess([NotNullWhen(true)] out T? data)
    {
        if (Success)
        {
            data = Response;
            return true;
        }

        data = default;
        return false;
    }
}

public sealed partial class VTubeStudio : ObservableObject
{
    public const int RequestTimeout = 300;
    public const ushort DefaultPort = 8001;

    public static readonly VTubeStudio Instance = new();

    [ObservableProperty] public partial ushort Port { get; set; } = DefaultPort;
    [ObservableProperty] public partial VTSStatus Status { get; private set; }
    [ObservableProperty] public partial string AccessToken { get; private set; } = string.Empty;

    readonly Channel<string> MessageSendQueue = Channel.CreateUnbounded<string>();
    // TODO: Add request timeouts.
    readonly ConcurrentDictionary<string, TaskCompletionSource<VTSSocket.ReceiveResult>> Requests = new();
    CancellationTokenSource? Identity;

    public async ValueTask<bool> Request(VTSRequestTemplate request)
    {
        return (await RequestInternal(request, static result => VTSRequestResult.FromResult(VTSPackets.DummyResponse))).Success;
    }

    public ValueTask<VTSRequestResult<T>> Request<T>(VTSRequestTemplate request) where T : VTSResponseTemplate
    {
        return RequestInternal(request, static result =>
        {
            T? packet = JsonSerializer.Deserialize<T>(result.Message.Span, VTSPackets.JsonOptions);
            if (packet is null) return VTSRequestResult<T>.Failed;
            return VTSRequestResult<T>.FromResult(packet);
        });
    }

    delegate VTSRequestResult<T> RequestProcessor<T>(VTSSocket.ReceiveResult result) where T : VTSResponseTemplate;
    async ValueTask<VTSRequestResult<T>> RequestInternal<T>(VTSRequestTemplate request, RequestProcessor<T> processor)
        where T : VTSResponseTemplate
    {
        if (typeof(T).IsAbstract)
            throw new ArgumentException($"Cannot use abstract classes in {nameof(VTubeStudio)}.{nameof(Request)} method!");
        if (Status != VTSStatus.Online)
            return VTSRequestResult<T>.Failed;

        TaskCompletionSource<VTSSocket.ReceiveResult> source = new();
        string requestID;

        try
        {
            const int ClashResolutionLimit = ushort.MaxValue;
            int ClashResolutionCounter = 0;
            while (true)
            {
                if (ClashResolutionCounter > ClashResolutionLimit)
                    return VTSRequestResult<T>.Failed;

                ClashResolutionCounter++;
                requestID = Random.Shared.NextInt64().ToString("X16");
                if (Requests.TryAdd(requestID, source))
                {
                    $"Enqueued RequestID: {requestID}".Out();
                    break;
                }
            }

            request.RequestID = requestID;
            string json = JsonSerializer.Serialize(request, request.GetType(), VTSPackets.JsonOptions);
            if (string.IsNullOrEmpty(json))
            {
                Requests.TryRemove(requestID, out _);
                return VTSRequestResult<T>.Failed;
            }

            $"Queueing JSON:\n{json}".Out();
            await MessageSendQueue.Writer.WriteAsync(json);
            using var cancellation = new CancellationTokenSource();
            using var registration = cancellation.Token.Register(() => source.TrySetCanceled());
            cancellation.CancelAfter(RequestTimeout);

            using var result = await source.Task;
            if (!result.Success)
                return VTSRequestResult<T>.Failed;

            return processor(result);
        }
        catch (OperationCanceledException) { /* Websocket connection was stopped. */ }
        catch (Exception ex)
        {
            ex.Out($"Request for ({typeof(T).Name}) failed!");
        }

        return VTSRequestResult<T>.Failed;
    }

    [RelayCommand] public void Start() => Application.Current.Dispatcher.Invoke(StartImmediate);
    void StartImmediate()
    {
        if (Status != VTSStatus.Offline) return;
        try
        {
            Communication(Identity = new());
            Status = VTSStatus.Pending;
            $"{this} Started.".Out();
        }
        catch (Exception ex)
        {
            ex.Out();
            Status = VTSStatus.Offline;
        }
    }

    [RelayCommand] public void Stop() => Application.Current.Dispatcher.Invoke(StopImmediate);
    void StopImmediate()
    {
        if (Status == VTSStatus.Offline) return;
        try
        {
            if (Identity is not null)
            {
                if (!Identity.IsCancellationRequested)
                    Identity.Cancel();
                Identity = null;
            }
            Status = VTSStatus.Offline;
            $"{this} Stopped.".Out();
        }
        catch (Exception ex)
        {
            ex.Out();
        }
    }

    private async void Communication(CancellationTokenSource identity)
    {
        VTubeStudioDiscovery.Instance.Request(this);
        CancellationToken token = identity.Token;
        VTSSocket? socket = null;
        try
        {
            // Reads app icon as Base64 to use as plugin preview.
            string image = string.Empty;
            string imageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
            if (File.Exists(imageFile))
            {
                byte[] bytes = await File.ReadAllBytesAsync(imageFile);
                image = Convert.ToBase64String(bytes);
            }

            // Fetches Port from a discovery server.
            TaskCompletionSource<ushort> PortSource = new();
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (VTubeStudioDiscovery.Instance.VTSActive)
                {
                    PortSource.TrySetResult(VTubeStudioDiscovery.Instance.VTSPort);
                    return;
                }

                VTubeStudioDiscovery.Instance.OnInformationUpdated += UpdateHandler;
                void UpdateHandler(VTubeStudioDiscovery service)
                {
                    if (service.VTSActive)
                    {
                        service.OnInformationUpdated -= UpdateHandler;
                        PortSource.TrySetResult(service.VTSPort);
                    }
                }
            });

            // This will prevent PortSource from disposing, but this should be fine.
            using (token.Register(() => PortSource.TrySetCanceled()))
            {
                await PortSource.Task;
            }
            VTubeStudioDiscovery.Instance.Release(this);

            // Constructs Port.
            if (identity.IsCancellationRequested) return;
            if (PortSource.Task.IsCanceled) return;
            Uri uri = new($"ws://localhost:{PortSource.Task.Result}");
            $"{this} URI Constructed successfully: {uri}".Out();

            // Establishes connection with a server.
            socket = new VTSSocket(identity, new());
            await socket.ConnectAsync(uri, token);
            ReportStatus(identity, VTSStatus.Online);
            $"{this} Successfully connected to VTuber Studio!".Out();

            // Initial connection.
            {
                string RequestID = $"ConnectionRequest{Random.Shared.Next():X8}";
                // Receive command should always be initialized first.
                var response = socket.ReceiveAsync<VTSAPIStateResponse>();
                await socket.SendAsync(new VTSAPIStateRequest()
                {
                    RequestID = RequestID,
                    Data = null
                });
                var packet = await response;
                $"Received {RequestID} result:\n{packet}".Out(ConsoleColor.Cyan);
            }

            // Using an existing token, if possible.
            bool authenticated = false;
            string authFile = Path.Combine(App.LocalAppDataFolder, "auth");
            token.ThrowIfCancellationRequested();
            if (File.Exists(authFile))
            {
                $"Restoring session from previous auth token...".Out();
                string auth;
                try
                {
                    auth = await File.ReadAllTextAsync(authFile);
                    authenticated = true;
                }
                catch (Exception ex)
                {
                    ex.Out($"Cannot read authentication token from '{authFile}'. New session auth token will be requested.\n");
                    goto TokenRestoreEnd;
                }

                var response = socket.ReceiveAsync<VTSAuthenticationResponse>();
                var RequestID = $"{nameof(VTSAuthenticationRequest)}{Random.Shared.Next():X8}";
                await socket.SendAsync(new VTSAuthenticationRequest()
                {
                    RequestID = RequestID,
                    Data = new()
                    {
                        PluginName = "Voice Trigger",
                        PluginDeveloper = "SANDCorp, SoG",
                        AuthenticationToken = auth,
                    }
                });
                var packet = await response;
                if (packet?.Data?.Authenticated == true)
                {
                    $"Plugin re-authentication successful! Previous session has been restored.".Out(ConsoleColor.Green);
                }
                else
                {
                    $"Cannot restore previous session from a response:\n{packet}".Out(ConsoleColor.Yellow);
                }

                TokenRestoreEnd:;
            }

            // Authentication.
            token.ThrowIfCancellationRequested();
            if (!authenticated)
            {
                $"Aquiring new Authentication Token...".Out();
                var response = socket.ReceiveAsync<VTSAuthenticationTokenResponse>();
                var RequestID = $"{nameof(VTSAuthenticationTokenRequest)}{Random.Shared.Next():X8}";
                await socket.SendAsync(new VTSAuthenticationTokenRequest()
                {
                    RequestID = RequestID,
                    Data = new()
                    {
                        PluginName = "Voice Trigger",
                        PluginDeveloper = "SANDCorp, SoG",
                        PluginIcon = image
                    }
                });
                var packet = await response;
                if (!string.IsNullOrEmpty(packet?.Data?.AuthenticationToken))
                {
                    authenticated = true;
                    string auth = packet.Data.AuthenticationToken;
                    try
                    {
                        await File.WriteAllTextAsync(authFile, auth);
                    }
                    catch (Exception ex)
                    {
                        ex.Out($"Cannot save authentication token. Session might not be remembered.");
                    }
                }
                else
                {
                    $"Failed to retrieve an authentication token!".Out(ConsoleColor.Yellow);
                }
            }

            if (!authenticated)
            {
                // TODO: Add communication restart function.
                //  Otherwise system is one failure away from requiring an app restart.
                $"Authentication failed! Communication will stop.".Out(ConsoleColor.Red);
                return;
            }

            // Requesting a list of models.
            string? loadedModelID = null;
            {
                $"Requesting a list of models...".Out();
                var response = socket.ReceiveAsync<VTSAvailableModelsResponse>();
                var RequestID = $"{nameof(VTSAvailableModelsRequest)}{Random.Shared.Next():X8}";
                await socket.SendAsync(new VTSAvailableModelsRequest()
                {
                    RequestID = RequestID,
                    Data = null,
                });
                var packet = await response;
                if (packet?.Data is not null)
                {
                    var item = packet.Data.AvailableModels?.FirstOrDefault(static d => d.ModelLoaded);
                    if (item.HasValue && item.Value.ModelLoaded)
                    {
                        loadedModelID = item.Value.ModelID;
                        $"Found an active model! (id: {loadedModelID}, name: {item.Value.ModelName})".Out(ConsoleColor.Green);
                    }
                    else
                    {
                        $"No loaded model found!".Out(ConsoleColor.Yellow);
                    }
                }
                else
                {
                    $"Failed to retrieve a list of models!".Out(ConsoleColor.Yellow);
                }
            }

            // Requesting a list of hotkeys.
            string? targetHotkeyID = null;
            if (!string.IsNullOrEmpty(loadedModelID))
            {
                $"Requesting a list of hotkeys...".Out();
                var response = socket.ReceiveAsync<VTSModelHotkeysResponse>();
                var RequestID = $"{nameof(VTSModelHotkeysRequest)}{Random.Shared.Next():X8}";
                await socket.SendAsync(new VTSModelHotkeysRequest()
                {
                    RequestID = RequestID,
                    Data = new()
                    {
                        ModelID = loadedModelID,
                        Live2DItemFileName = null,
                    }
                });
                var packet = await response;
                if (packet?.Data is not null)
                {
                    var item = packet.Data.AvailableHotkeys?.FirstOrDefault(static d => d.Name == "粉双马尾");
                    if (item.HasValue && item.Value.Name == "粉双马尾")
                    {
                        targetHotkeyID = item.Value.HotkeyID;
                        $"Hotkey found! (id: {targetHotkeyID}, name: {item.Value.Name})".Out(ConsoleColor.Green);
                    }
                    else
                    {
                        $"Failed to find a target hotkey!".Out();
                    }
                }
                else
                {
                    $"Failed to retrieve a list of hotkeys!".Out(ConsoleColor.Yellow);
                }
            }

            // Requesting execution of one of them.
            if (!string.IsNullOrEmpty(targetHotkeyID))
            {
                $"Requesting a hotkey execution...".Out();
                var response = socket.ReceiveAsync<VTSHotkeyTriggerResponse>();
                var RequestID = $"{nameof(VTSHotkeyTriggerRequest)}{Random.Shared.Next():X8}";
                await socket.SendAsync(new VTSHotkeyTriggerRequest()
                {
                    RequestID = RequestID,
                    Data = new()
                    {
                        HotkeyID = targetHotkeyID,
                        ItemInstanceID = null,
                    }
                });
                var packet = await response;
                if (!string.IsNullOrEmpty(packet?.Data?.HotkeyID))
                {
                    $"Hotkey triggered successfully!".Out(ConsoleColor.Green);
                }
                else
                {
                    $"Failed to trigger a hotkey!".Out(ConsoleColor.Yellow);
                }
            }

            async Task SocketListened()
            {
                while (!token.IsCancellationRequested)
                {
                    int fails = 0;
                    try
                    {
                        var result = await socket.ReceiveAsync();
                        if (!result.Success)
                        {
                            const int MaxRestarts = 20;
                            if (fails >= MaxRestarts)
                            {
                                $"({fails}/{MaxRestarts}) Receive restarts failed! Closing the connection...".Out();
                                Stop();
                                break;
                            }

                            $"Receive failed! Starting a short delay before retrying.".Out();
                            fails++;
                            await Task.Delay(500);
                            continue;
                        }

                        bool queued = false;
                        try
                        {
                            var response = JsonSerializer.Deserialize<VTSResponse>(result.Message.Span, VTSPackets.JsonOptions);
                            $"Reading template:\n{response}".Out(ConsoleColor.Cyan);
                            if (response?.RequestID is null)
                            {
                                $"Cannot deserialize basic response data! VTS Json: {new string(result.Message.Span)}".Out(ConsoleColor.Yellow);
                            }
                            else if (Requests.TryRemove(response.RequestID, out var receiver))
                            {
                                receiver.TrySetResult(result);
                                queued = true;
                            }
                            else
                            {
                                $"Cannot find a handler for RequestID: {response.RequestID}".Out(ConsoleColor.Yellow);
                            }
                        }
                        finally
                        {
                            if (!queued)
                                result.Dispose();
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        ex.Out();
                    }
                }
            }

            async Task SocketSender()
            {
                await foreach (var json in MessageSendQueue.Reader.ReadAllAsync(token))
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        await socket.SendJsonAsync(json);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        ex.Out();
                    }
                }
            }

            _ = SocketListened();
            _ = SocketSender();
            TaskCompletionSource finish = new();
            using (token.Register(finish.SetResult))
            {
                await finish.Task;
            }
        }
        catch (Exception ex)
        {
            // TODO: Toast failure on UI.
            //  Prompt them to enable API access.
            ex.Out(ToString());
            identity.Cancel();
        }
        finally
        {
            VTubeStudioDiscovery.Instance.Release(this);
            ReportStatus(identity, VTSStatus.Offline);
            socket?.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReportStatus(CancellationTokenSource identity, VTSStatus status)
    {
        Application.Current.Dispatcher.Invoke(() => ReportStatusImmediate(identity, status));
    }
    private void ReportStatusImmediate(CancellationTokenSource identity, VTSStatus status)
    {
        if (Identity != identity) return;
        try
        {
            Status = status;
        }
        catch (Exception ex)
        {
            ex.Out();
        }
    }

    public override string ToString() => $"[{nameof(VTubeStudio)}]";
}

public sealed partial class VTuberModel : ObservableObject
{
    public const string DefaultName = "Unknown";

    [ObservableProperty] public partial string Name { get; set; } = DefaultName;
    [ObservableProperty] public partial HokeyViewModel[] Hotkeys { get; set; } = [];


}

public sealed partial class HokeyViewModel : ObservableObject
{

}