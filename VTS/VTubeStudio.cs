using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using VoiceTrigger.Extensions;
using VoiceTrigger.VTS.Events;
using VoiceTrigger.VTS.Packets;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

public sealed partial class VTubeStudio : ObservableObject
{
    public static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DefaultAuthTimeout = Timeout.InfiniteTimeSpan;
    public static readonly TimeSpan DefaultReAuthTimeout = TimeSpan.FromSeconds(15);

    public static readonly VTubeStudio Instance = new();
    static readonly string AuthFilePath = Path.Combine(App.LocalAppDataFolder, "auth");

    public event Action? OnAuthenticated;
    public event Action? OnUnauthenticated;
    public event Action<bool>? OnAuthenticationChanged;

    public VTubeStudioEvents Events { get; init; } = new();
    [ObservableProperty] public partial VTSStatus Status { get; private set; }
    [ObservableProperty] public partial bool Authenticated { get; private set; }

    readonly ConcurrentDictionary<string, TaskCompletionSource<VTSSocket.ReceiveResult>> Requests = new();
    readonly Channel<PacketData> SendQueue = Channel.CreateUnbounded<PacketData>();
    readonly Lock SocketLock = new();
    VTSSocket? Socket;

    public readonly record struct PacketData(string RequestID, string Json);

    public VTubeStudio()
    {
        Events.Map<VTSTestEvent>("TestEvent");
        Events.Map<VTSModelLoadedEvent>("ModelLoadedEvent");
        Events.Map<VTSHotkeyTriggeredEvent>("HotkeyTriggeredEvent");
    }

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
                    socket = Socket;
                    Socket = null;
                }
            }
        }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { SocketLock.Exit(); }
    }

    void SetStatus(VTSSocket socket, VTSStatus status) => Application.Current.Dispatcher.Invoke(() => SetStatusImmediate(socket, status));
    void SetStatusImmediate(VTSSocket socket, VTSStatus status)
    {
        if (Socket != socket) return;
        try
        {
            Status = status;
        }
        catch (Exception ex) { ex.Out(ToString()); }
    }

    async void Connect(VTSSocket socket)
    {
        // TODO fix issues - check logs or wait for 20s before approving authentuication request.
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

                    var result = await SystemRequest<VTSAuthenticationResponse>(new VTSAuthenticationRequest
                    {
                        Data = new()
                        {
                            PluginName = "Voice Trigger",
                            PluginDeveloper = "Sandcorp, SoG",
                            AuthenticationToken = auth,
                        }
                    }, DefaultReAuthTimeout);
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

                    $"{this} Acquiring new Authentication Token...".Out();
                    var result = await SystemRequest<VTSAuthenticationTokenResponse>(new VTSAuthenticationTokenRequest
                    {
                        Data = new()
                        {
                            PluginName = "Voice Trigger",
                            PluginDeveloper = "Sandcorp, SoG",
                            PluginIcon = image
                        }
                    }, DefaultAuthTimeout);
                    if (result.ResolveSuccess(out var response) && !string.IsNullOrEmpty(response.Data?.AuthenticationToken))
                    {
                        authenticated = true;
                        string auth = response.Data.AuthenticationToken;
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(AuthFilePath) ?? string.Empty);
                            await File.WriteAllTextAsync(AuthFilePath, auth);
                            $"{this} New Auth token saved successfully!".Out();

                            var authResult = await SystemRequest<VTSAuthenticationResponse>(new VTSAuthenticationRequest
                            {
                                Data = new()
                                {
                                    PluginName = "Voice Trigger",
                                    PluginDeveloper = "Sandcorp, SoG",
                                    AuthenticationToken = auth,
                                }
                            }, DefaultReAuthTimeout);
                            if (authResult.ResolveSuccess(out var authResponse) && authResponse.Data?.Authenticated == true)
                            {
                                authenticated = true;
                                $"{this} Plugin authentication successful! New session started!".Out(ConsoleColor.Green);
                            }
                            else
                            {
                                $"{this} Cannot authenticate with a new session token!\n{authResponse}".Out(ConsoleColor.Yellow);
                            }
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
                    $"{this} Authentication failed! Connection stopped and can be restarted.".Out(ConsoleColor.Red);
                    break;
                }

                $"{this} Requesting common events...".Out();
                /*try
                {
                    $"Requesting test event...".Out();
                    var response = await SystemRequest<VTSEventSubscriptionResponse>(new VTSEventSubscriptionRequest
                    {
                        Data = new()
                        {
                            EventName = "TestEvent",
                            Subscribe = true,
                            Config = new VTSECTest()
                            {
                                TestMessageForEvent = "test message",
                            }
                        }
                    });
                    if (!response.Success)
                    {
                        "Subscription failed! Restarting soon.".Out(); break;
                    }
                    $"Subscription successful!".Out();
                }
                catch (Exception ex) { ex.Out("Subscription failed! Restarting soon."); break; }*/

                try
                {
                    $"Requesting model loaded event...".Out();
                    var response = await SystemRequest<VTSEventSubscriptionResponse>(new VTSEventSubscriptionRequest
                    {
                        Data = new()
                        {
                            EventName = "ModelLoadedEvent",
                            Subscribe = true,
                            Config = new VTSECModelLoaded(),
                        }
                    }, DefaultRequestTimeout);
                    if (!response.Success)
                    {
                        "Subscription failed! Restarting soon.".Out(); break;
                    }
                    $"Subscription successful!".Out();
                }
                catch (Exception ex) { ex.Out("Subscription failed! Restarting soon."); break; }

                try
                {
                    $"Requesting hotkey triggered event...".Out();
                    var response = await SystemRequest<VTSEventSubscriptionResponse>(new VTSEventSubscriptionRequest
                    {
                        Data = new()
                        {
                            EventName = "HotkeyTriggeredEvent",
                            Subscribe = true,
                            Config = new VTSECHotkeyTriggered()
                            {
                                IgnoreHotkeysTriggeredByAPI = false,
                                OnlyForAction = string.Empty,
                            }
                        }
                    }, DefaultRequestTimeout);
                    if (!response.Success)
                    {
                        "Subscription failed! Restarting soon.".Out(); break;
                    }
                    $"Subscription successful!".Out();
                }
                catch (Exception ex) { ex.Out("Subscription failed! Restarting soon."); break; }
                $"{this} Requests successful!".Out();

                $"{this} Authentication is now complete!".Out(ConsoleColor.Green);
                SetStatus(socket, VTSStatus.Authenticated);

                // Lets user use the socket until any issues happen.
                await Task.WhenAll(a, b);
                //"Remote has quit".Out(ConsoleColor.Red);
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

        try
        {
            var list = Requests.ToList();
            Requests.Clear();
            foreach (var request in list)
                request.Value.TrySetResult(VTSSocket.ReceiveResult.Faulted);
        }
        catch (Exception ex) { ex.Out(); }

        socket.Dispose();
        SetStatus(socket, VTSStatus.Offline);
        StopInternal(socket);
        $"{this} {nameof(VTSSocket)} stopped.".Out(ConsoleColor.Yellow);
    }

    async Task<ushort> DiscoverPort(CancellationToken token)
    {
        $"{this} Discovering port...".Out(ConsoleColor.Yellow);
        using var scope = VTSDiscovery.Instance.RequestScope(this);
        TaskCompletionSource<ushort> PortSource = new();
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (VTSDiscovery.Instance.VTSActive)
            {
                PortSource.TrySetResult(VTSDiscovery.Instance.VTSPort);
                return;
            }

            VTSDiscovery.Instance.OnInformationUpdated += UpdateHandler;
            void UpdateHandler(VTSDiscovery service)
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
                if (socket.WebSocket.State != WebSocketState.Open) break;
                if (!result.Success)
                {
                    $"{this} Receive failed!".Out(ConsoleColor.Yellow);
                    await Task.Delay(100, token);
                    $"{this} Restarting receive handler...".Out();
                    continue;
                }

                using JsonDocument doc = JsonDocument.Parse(result.Message, VTSPackets.DocumentOptions);
                if (Events.TryFire(doc))
                    continue;

                if (doc.RootElement.TryGetProperty(VTSPackets.RequestIDJsonPropertyName, out var element))
                {
                    if (element.ValueKind != JsonValueKind.String)
                    {
                        $"RequestID field type is not String! Json payload is ignored.".Out(ConsoleColor.Yellow);
                        continue;
                    }

                    string? requestID = element.GetString();
                    if (string.IsNullOrEmpty(requestID))
                    {
                        $"Received empty RequestID ({requestID}). Json payload is ignored.".Out(ConsoleColor.Yellow);
                        continue;
                    }
                    if (Requests.TryRemove(requestID, out var receiver))
                    {
                        if (receiver.TrySetResult(result))
                        {
                            result = default;
                        }
                        else
                        {
                            $"Cannot set request result for RequestID: {requestID}. It's likely that it timed out.".Out(ConsoleColor.Yellow);
                        }
                    }
                    else
                    {
                        $"Cannot find a receiver for RequestID: {requestID}. It's likely that it timed out.".Out(ConsoleColor.Yellow);
                    }
                }
            }
            catch (JsonException) { /* Simply does for the next receive request. */ }
            catch (WebSocketException) { break; }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(ToString()); break; }
            finally { result.Dispose(); }
        }

        bool graceful = true;
        try
        {
            if (!socket.Identity.IsCancellationRequested)
                socket.Identity.Cancel(); // Lets socket quit gracefully.
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            ex.Out("Receive handler stopped, but wasn't able to cancel the socket gracefully.", ConsoleColor.Yellow);
            graceful = false;
        }
        if (graceful) $"{this} Send handler Stopped gracefully.".Out();
    }

    async Task Send(VTSSocket socket)
    {
        $"{this} Starting Send handler...".Out();
        CancellationToken token = socket.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var packet = await SendQueue.Reader.ReadAsync(token);
                try
                {
                    token.ThrowIfCancellationRequested();
                    await socket.SendAsync(packet.Json);
                }
                catch (WebSocketException)
                {
                    if (Requests.TryGetValue(packet.RequestID, out var request))
                        request.TrySetResult(VTSSocket.ReceiveResult.Faulted);
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ex.Out(ToString()); }

        bool graceful = true;
        try
        {
            if (!socket.Identity.IsCancellationRequested)
                socket.Identity.Cancel(); // Lets socket quit gracefully.
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            ex.Out("Send handler stopped, but wasn't able to cancel the socket gracefully.", ConsoleColor.Yellow);
            graceful = false;
        }
        if (graceful) $"{this} Send handler Stopped gracefully.".Out();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> Request(VTSRequestTemplate request) => Request(request, DefaultRequestTimeout);
    public ValueTask<bool> Request(VTSRequestTemplate request, TimeSpan timeout)
    {
        if (Status != VTSStatus.Authenticated)
            return ValueTask.FromResult(false);
        return SystemRequest(request, timeout);
    }

    /// <summary>
    /// Allows communication before authenticated.
    /// </summary>
    async ValueTask<bool> SystemRequest(VTSRequestTemplate request, TimeSpan timeout)
    {
        return (await SystemRequestInternal(request, timeout,
            static result => VTSRequestResult.FromResult(VTSPackets.DummyResponse))).Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<VTSRequestResult<T>> Request<T>(VTSRequestTemplate request) where T : VTSResponseTemplate
    {
        return Request<T>(request, DefaultRequestTimeout);
    }
    public ValueTask<VTSRequestResult<T>> Request<T>(VTSRequestTemplate request, TimeSpan timeout) where T : VTSResponseTemplate
    {
        if (Status != VTSStatus.Authenticated)
            return ValueTask.FromResult(VTSRequestResult<T>.Failed);
        return SystemRequest<T>(request, timeout);
    }

    /// <summary>
    /// Allows communication before authenticated.
    /// </summary>
    ValueTask<VTSRequestResult<T>> SystemRequest<T>(VTSRequestTemplate request, TimeSpan timeout) where T : VTSResponseTemplate
    {
        return SystemRequestInternal(request, timeout, static result =>
        {
            T? packet = JsonSerializer.Deserialize<T>(result.Message.Span, VTSPackets.JsonOptions);
            if (packet is null) return VTSRequestResult<T>.Failed;
            return VTSRequestResult<T>.FromResult(packet);
        });
    }

    delegate VTSRequestResult<T> RequestProcessor<T>(VTSSocket.ReceiveResult result) where T : VTSResponseTemplate;
    async ValueTask<VTSRequestResult<T>> SystemRequestInternal<T>(VTSRequestTemplate request, TimeSpan timeout, RequestProcessor<T> processor)
        where T : VTSResponseTemplate
    {
        Status.Out("Status: ");
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

            // TODO: Add custom timeout support for requests.
            await SendQueue.Writer.WriteAsync(new(requestID, json));
            if (timeout.IsFinite())
            {
                using var cancellation = new CancellationTokenSource(timeout);
                using var registration = cancellation.Token.Register(() => { "Timeout!".Out(ConsoleColor.Yellow); source.TrySetCanceled(); });
            }

            using var result = await source.Task;
            if (!result.Success)
            {
                $"Failed!".Out();
                return VTSRequestResult<T>.Failed;
            }

            return processor(result);
        }
        catch (OperationCanceledException) { /* WebSocket connection was stopped. */ }
        catch (Exception ex)
        {
            ex.Out($"Request for ({typeof(T).Name}) failed!");
        }
        finally
        {
            if (requestID is not null)
                if (Requests.TryRemove(requestID, out var src))
                    src.TrySetCanceled();
        }

        return VTSRequestResult<T>.Failed;
    }

    public override string ToString() => $"[{nameof(VTubeStudio)}]";
}