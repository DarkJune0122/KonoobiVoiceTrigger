using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using VoiceTrigger.VTS.Packets;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

public sealed partial class VTubeStudio : ObservableObject
{
    public const int RequestTimeout = 5000;
    public const ushort DefaultPort = 8001;

    public static readonly VTubeStudio Instance = new();
    static readonly string AuthFilePath = Path.Combine(App.LocalAppDataFolder, "auth");

    public event Action? OnAuthenticated;
    public event Action? OnUnauthenticated;
    public event Action<bool>? OnAuthenticationChanged;

    [ObservableProperty] public partial VTSStatus Status { get; private set; }
    [ObservableProperty] public partial bool Authenticated { get; private set; }

    readonly ConcurrentDictionary<string, TaskCompletionSource<VTSSocket.ReceiveResult>> Requests = new();
    readonly Channel<string> SendQueue = Channel.CreateUnbounded<string>();
    readonly Lock SocketLock = new();
    VTSSocket? Socket;

    partial void OnStatusChanged(VTSStatus value) => Authenticated = value == VTSStatus.Authenticated;
    partial void OnAuthenticatedChanged(bool value)
    {
        OnAuthenticationChanged?.Invoke(value);
        if (value) OnAuthenticated?.Invoke();
        else OnUnauthenticated?.Invoke();
    }

    [RelayCommand]
    public void Start()
    {
        SocketLock.Enter();
        try
        {
            if (Socket is null)
                Connect(Socket = new());
        }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { SocketLock.Exit(); }
    }

    [RelayCommand]
    public void Stop() => StopInternal(null);
    void StopInternal(VTSSocket? socket)
    {
        SocketLock.Enter();
        try
        {
            if (Socket == socket || socket is null)
            {
                if (Socket is not null)
                {
                    if (Socket.Identity.IsCancellationRequested)
                        Socket.Identity.Cancel(); // Lets socket quit gracefully.
                    Socket = null;
                }
            }
        }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { SocketLock.Exit(); }
    }

    void SetStatus(VTSSocket socket, VTSStatus status) => Application.Current.Dispatcher.Invoke(() => SetStatusImmediate(socket, status));
    void SetStatusImmediate(VTSSocket socjet, VTSStatus status)
    {
        if (Socket != socjet) return;
        try
        {
            Status = status;
        }
        catch (Exception ex) { ex.Out(ToString()); }
    }

    async void Connect(VTSSocket socket)
    {
        $"{this} {nameof(VTSSocket)} started.".Out();
        SetStatus(socket, VTSStatus.Pending);
        CancellationToken token = socket.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Constructs URI.
                var port = await DiscoverPort(token);
                Uri uri = new($"ws://localhost:{port}");
                $"{this} URI Constructed successfully: {uri}".Out();

                // Establishes connection with a server.
                await socket.ConnectAsync(uri);
                SetStatus(socket, VTSStatus.Online);
                $"{this} Successfully connected to VTube Studio!".Out(ConsoleColor.Green);

                Task a = Receive(socket);
                Task b = Send(socket);

                // Authentication:
                // Using an existing token, if possible.
                bool authenticated = false;
                token.ThrowIfCancellationRequested();
                if (File.Exists(AuthFilePath))
                {
                    $"{this} Restoring session from previous auth token...".Out();
                    string auth;
                    try
                    {
                        auth = await File.ReadAllTextAsync(AuthFilePath, token);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        ex.Out($"{this} Cannot read authentication token from '{AuthFilePath}'. New session auth token will be requested.\n");
                        goto TokenRestoreEnd;
                    }

                    var result = await Request<VTSAuthenticationResponse>(new VTSAuthenticationRequest
                    {
                        Data = new()
                        {
                            PluginName = "Voice Trigger",
                            PluginDeveloper = "SANDCorp, SoG",
                            AuthenticationToken = auth,
                        }
                    });
                    if (result.ResolveSuccess(out var response) && response.Data?.Authenticated == true)
                    {
                        authenticated = true;
                        $"{this} Plugin re-authentication successful! Previous session has been restored.".Out(ConsoleColor.Green);
                    }
                    else
                    {
                        $"{this} Cannot restore previous session from a response:\n{response}".Out(ConsoleColor.Yellow);
                    }

                    TokenRestoreEnd:;
                }

                // Authentication.
                if (!authenticated)
                {
                    // Reads app icon as Base64 to use as plugin preview.
                    string image = string.Empty;
                    try
                    {
                        string imageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                        if (File.Exists(imageFile))
                        {
                            byte[] bytes = await File.ReadAllBytesAsync(imageFile, token);
                            image = Convert.ToBase64String(bytes);
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        ex.Out($"{this} Cannot load-in the image for a plugin.");
                    }

                    $"{this} Aquiring new Authentication Token...".Out();
                    var result = await Request<VTSAuthenticationTokenResponse>(new VTSAuthenticationTokenRequest
                    {
                        Data = new()
                        {
                            PluginName = "Voice Trigger",
                            PluginDeveloper = "SANDCorp, SoG",
                            PluginIcon = image
                        }
                    });
                    if (result.ResolveSuccess(out var response) && !string.IsNullOrEmpty(response.Data?.AuthenticationToken))
                    {
                        authenticated = true;
                        string auth = response.Data.AuthenticationToken;
                        try
                        {
                            await File.WriteAllTextAsync(AuthFilePath, auth);
                        }
                        catch (Exception ex)
                        {
                            ex.Out($"{this} Cannot save authentication token. Session might not be remembered.");
                        }
                    }
                    else
                    {
                        $"{this} Failed to retrieve an authentication token!".Out(ConsoleColor.Yellow);
                    }
                }

                if (!authenticated)
                {
                    $"{this} Authentication failed! Communication will stop.".Out(ConsoleColor.Red);
                    return;
                }

                SetStatus(socket, VTSStatus.Authenticated);

                // Lets user use the socket until any issues happen.
                await Task.WhenAll(a, b);
                break;
            }
            catch (WebSocketException) { break; }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(ToString()); }

            const int RestartDelayMs = 500;
            SetStatus(socket, VTSStatus.Pending);
            $"{this} Restarting {nameof(VTSSocket)} after {RestartDelayMs} ms.".Out();
            try
            {
                await Task.Delay(RestartDelayMs, token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out($"{this} Unknown exception."); break; }
            $"{this} Restarting {nameof(VTSSocket)}...".Out();
        }

        SetStatus(socket, VTSStatus.Offline);
        socket.Dispose();
        StopInternal(socket);
        $"{this} {nameof(VTSSocket)} stopped.".Out();
    }

    async Task<ushort> DiscoverPort(CancellationToken token)
    {
        $"{this} Discovering port...".Out(ConsoleColor.Yellow);
        using var scope = VTubeStudioDiscovery.Instance.RequestScope(this);
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

        using (token.Register(() => PortSource.TrySetCanceled()))
        {
            await PortSource.Task;
        }

        token.ThrowIfCancellationRequested();
        $"{this} Port discovered! Port: {PortSource.Task.Result}".Out();
        return PortSource.Task.Result;
    }

    async Task Receive(VTSSocket socket)
    {
        $"{this} Starting Receive handler...".Out();
        CancellationToken token = socket.Token;
        while (!token.IsCancellationRequested)
        {
            VTSSocket.ReceiveResult result = default;
            try
            {
                result = await socket.ReceiveAsync();
                if (!result.Success)
                {
                    $"{this} Receive failed!".Out(ConsoleColor.Yellow);
                    if (socket.WebSocket.State != WebSocketState.Open) break;
                    else
                    {
                        await Task.Delay(100, token);
                        $"{this} Restarting receive handler...".Out();
                        continue;
                    }
                }

                var packet = JsonSerializer.Deserialize<VTSResponse>(result.Message, VTSPackets.JsonOptions);
                if (packet is null || packet.RequestID == default)
                {
                    $"Cannot read RequestID from input data: {result.Message}".Out(ConsoleColor.Yellow);
                }
                else if (Requests.TryRemove(packet.RequestID, out var receiver) && receiver.TrySetResult(result))
                {
                    if (receiver.TrySetResult(result))
                    {
                        result = default;
                    }
                    else
                    {
                        $"Cannot set request result for RequestID: {packet.RequestID}. It's likely that it timed out.".Out(ConsoleColor.Yellow);
                    }
                }
                else
                {
                    $"Cannot find a receiver for RequestID: {packet.RequestID}. It's likely that it timed out.".Out(ConsoleColor.Yellow);
                }
            }
            catch (JsonException) { /* Simply does for the next receive request. */ }
            catch (WebSocketException) { break; }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(ToString()); break; }
            finally { result.Dispose(); }
        }
        $"{this} Receive handler Stopped gracefully.".Out();
        StopInternal(socket);
    }

    async Task Send(VTSSocket socket)
    {
        $"{this} Starting Send handler...".Out();
        CancellationToken token = socket.Token;
        await foreach (var json in SendQueue.Reader.ReadAllAsync(token))
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await socket.SendAsync(json);
            }
            catch (WebSocketException) { break; }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(ToString()); break; }
        }
        $"{this} Send handler Stopped gracefully.".Out();
        StopInternal(socket);
    }

    public async ValueTask<bool> Request(VTSRequestTemplate request)
    {
        return (await RequestInternal(request, static result => VTSRequestResult.FromResult(VTSPackets.DummyResponse))).Success;
    }

    public ValueTask<VTSRequestResult<T>> Request<T>(VTSRequestTemplate request) where T : VTSResponseTemplate
    {
        return RequestInternal(request, static result =>
        {
            T? packet = JsonSerializer.Deserialize<T>(result.Message, VTSPackets.JsonOptions);
            if (packet is null) return VTSRequestResult<T>.Failed;
            return VTSRequestResult<T>.FromResult(packet);
        });
    }

    delegate VTSRequestResult<T> RequestProcessor<T>(VTSSocket.ReceiveResult result) where T : VTSResponseTemplate;
    async ValueTask<VTSRequestResult<T>> RequestInternal<T>(VTSRequestTemplate request, RequestProcessor<T> processor)
        where T : VTSResponseTemplate
    {
        if (typeof(T).IsAbstract)
            throw new ArgumentException($"Cannot use abstract class ({typeof(T).Name}) in {nameof(VTubeStudio)}.{nameof(Request)} method!");
        if (Status == VTSStatus.Offline)
            return VTSRequestResult<T>.Failed;

        TaskCompletionSource<VTSSocket.ReceiveResult> source = new();
        string? requestID = null;

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
                if (requestID == default) continue;
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
                return VTSRequestResult<T>.Failed;
            }

            await SendQueue.Writer.WriteAsync(json);
            using var cancellation = new CancellationTokenSource(RequestTimeout);
            using var registration = cancellation.Token.Register(() => { "Timeout!".Out(ConsoleColor.Yellow); source.TrySetCanceled(); });
            using var result = await source.Task;
            if (!result.Success)
            {
                $"Failed!".Out();
                return VTSRequestResult<T>.Failed;
            }

            return processor(result);
        }
        catch (OperationCanceledException) { /* Websocket connection was stopped. */ }
        catch (Exception ex)
        {
            ex.Out($"Request for ({typeof(T).Name}) failed!");
        }
        finally
        {
            if (requestID is not null)
                Requests.TryRemove(requestID, out _);
        }

        return VTSRequestResult<T>.Failed;
    }

    public override string ToString() => $"[{nameof(VTubeStudio)}]";
}

/*
// Requesting a list of models.
string? loadedModelID = null;
{
    $"Requesting a list of models...".Out();
    var result = await Request<VTSAvailableModelsResponse>(new VTSAvailableModelsRequest());
    if (result.ResolveSuccess(out var response))
    {
        var item = response.Data?.AvailableModels?.FirstOrDefault(static d => d.ModelLoaded);
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
    var result = await Request<VTSModelHotkeysResponse>(new VTSModelHotkeysRequest()
    {
        Data = new()
        {
            ModelID = loadedModelID,
            Live2DItemFileName = null,
        }
    });
    if (result.ResolveSuccess(out var response))
    {
        var item = response.Data?.AvailableHotkeys?.FirstOrDefault(static d => d.Name == "粉双马尾");
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
    var result = await Request<VTSHotkeyTriggerResponse>(new VTSHotkeyTriggerRequest()
    {
        Data = new()
        {
            HotkeyID = targetHotkeyID,
            ItemInstanceID = null,
        }
    });
    if (result.ResolveSuccess(out var response) && !string.IsNullOrEmpty(response.Data?.HotkeyID))
    {
        $"Hotkey triggered successfully!".Out(ConsoleColor.Green);
    }
    else
    {
        $"Failed to trigger a hotkey!".Out(ConsoleColor.Yellow);
    }
}
 */