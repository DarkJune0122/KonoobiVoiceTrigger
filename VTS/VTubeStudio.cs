using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Markup;
using VoiceTrigger.VTS.Packets;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VoiceTrigger;

public enum ConnectionStatus : byte
{
    Offline,
    Pending,
    Online,
}

public sealed partial class VTubeStudioDiscovery : ObservableObject
{
    public static readonly VTubeStudioDiscovery Instance = new();

    public delegate void UpdateEventHandler(VTubeStudioDiscovery service);
    public event UpdateEventHandler? OnInformationUpdated;

    [ObservableProperty] public partial ConnectionStatus Status { get; private set; }
    [ObservableProperty] public partial ushort Port { get; set; } = 47779;
    [ObservableProperty] public partial bool VTSActive { get; private set; } = false;
    [ObservableProperty] public partial ushort VTSPort { get; private set; } = 0;
    [ObservableProperty] public partial string VTSInstanceID { get; private set; } = string.Empty;
    [ObservableProperty] public partial string VTSWindowTitle { get; private set; } = string.Empty;

    readonly HashSet<object> Requests = [];
    CancellationTokenSource? Identity;

    public void Request(object user)
    {
        lock (Requests)
        {
            if (Requests.Count == 0 && Requests.Add(user))
                Application.Current.Dispatcher.Invoke(StartImmediate);
        }
    }

    public void Release(object user)
    {
        lock (Requests)
        {
            if (Requests.Remove(user) && Requests.Count == 0)
                Application.Current.Dispatcher.Invoke(StopImmediate);
        }
    }

    //[RelayCommand] public void Start() => Application.Current.Dispatcher.Invoke(StartImmediate);
    private void StartImmediate()
    {
        if (Status != ConnectionStatus.Offline) return;
        try
        {
            Communication(Port, Identity = new());
            Status = ConnectionStatus.Pending;
            $"{this} Started.".Out();
        }
        catch (Exception ex)
        {
            ex.Out(ToString());
        }
    }

    //[RelayCommand] public void Stop() => Application.Current.Dispatcher.Invoke(StopImmediate);
    private void StopImmediate()
    {
        if (Status == ConnectionStatus.Offline) return;
        try
        {
            Identity?.Cancel();
            Identity = null;
            Status = ConnectionStatus.Offline;
            $"{this} Stopped.".Out();
        }
        catch (Exception ex)
        {
            ex.Out(ToString());
        }
    }

    private async void Communication(ushort port, CancellationTokenSource identity)
    {
        CancellationToken token = identity.Token;
        UdpClient client = new(port);
        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await client.ReceiveAsync(token);
                ReportStatus(identity, ConnectionStatus.Online);

                var data = JsonSerializer.Deserialize<VTSDiscoveryResponse>(result.Buffer)?.Data;
                if (data is null)
                {
                    $"{this} Cannot deserialize '{nameof(VTSDiscoveryResponse)}.{nameof(VTSDiscoveryResponse.Data)}' field!".Out(ConsoleColor.Yellow);
                    continue;
                }

                $"{this} Received Discovery data:\n{data}".Out(ConsoleColor.Gray);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Note: Running multiple VTuber studios at the same time is not supported at the moment.
                    VTSActive = data.Active;
                    VTSPort = data.Port;
                    VTSInstanceID = data.InstanceID ?? string.Empty;
                    VTSWindowTitle = data.WindowTitle ?? string.Empty;
                    OnInformationUpdated?.Invoke(this);
                });

                $"{this} Discovery succeeded. Increasing delays between checks.".Out();
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                ReportStatus(identity, ConnectionStatus.Pending);
                ex.Out(ToString());
                await Task.Delay(2000);
                $"{this} Restarting...".Out();
            }
            await Task.Delay(300);
        }

        ReportStatus(identity, ConnectionStatus.Offline);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReportStatus(CancellationTokenSource identity, ConnectionStatus status)
    {
        Application.Current.Dispatcher.Invoke(() => ReportStatusImmediate(identity, status));
    }
    private void ReportStatusImmediate(CancellationTokenSource identity, ConnectionStatus status)
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

    public override string ToString() => $"[{nameof(VTubeStudioDiscovery)}]";
}

public sealed class VTSDiscoveryResponse : VTSResponse<VTSDiscoveryResponseData>;
public sealed class VTSDiscoveryResponseData : VTSPacketData
{
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("port")] public ushort Port { get; set; }
    [JsonPropertyName("instanceID")] public string? InstanceID { get; set; }
    [JsonPropertyName("windowTitle")] public string? WindowTitle { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix, bool newLine = DefaultNewLine)
    {
        Append(b, prefix, Active);
        Append(b, prefix, Port);
        Append(b, prefix, InstanceID);
        Append(b, prefix, WindowTitle, newLine);
        return b;
    }
}

public sealed class VTSAPIStateRequest : VTSRequest
{
    public override string? MessageType { get; set; } = "APIStateRequest";
}

public sealed class VTSAPIStateResponse : VTSResponse<VTSAPIStateResposeData>;
public sealed class VTSAPIStateResposeData : VTSPacketData
{
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("vTubeStudioVersion")] public string? VTubeStudioVersion { get; set; }
    [JsonPropertyName("currentSessionAuthenticated")] public bool CurrentSessionAuthenticated { get; set; }
}

public sealed class VTSAuthenticationTokenRequest : VTSRequest<VTSAuthenticationTokenRequestData>
{
    public override string? MessageType { get; set; } = "AuthenticationTokenRequest";
}
public sealed class VTSAuthenticationTokenRequestData : VTSPacketData
{
    [JsonPropertyName("pluginName")] public required string? PluginName { get; set; }
    [JsonPropertyName("pluginDeveloper")] public required string? PluginDeveloper { get; set; }
    [JsonPropertyName("pluginIcon")] public required string? PluginIcon { get; set; }

    public override StringBuilder ToString(StringBuilder b, string prefix = DefaultPrefix, bool newLine = DefaultNewLine)
    {
        b.Append(prefix); b.Append($"{nameof(PluginName)}: "); b.AppendLine(PluginName);
        b.Append(prefix); b.Append($"{nameof(Port)}: "); b.AppendLine(Port.ToString());
        b.Append(prefix); b.Append($"{nameof(InstanceID)}: "); b.AppendLine(InstanceID);
        b.Append(prefix); b.Append($"{nameof(WindowTitle)}: "); b.Append(WindowTitle);
        if (newLine) b.AppendLine();
        return b;
    }
}

public sealed class VTSAuthenticationTokenResponse : VTSResponse<VTSAuthenticationTokenResponseData>;
public sealed class VTSAuthenticationTokenResponseData : VTSPacketData
{
    [JsonPropertyName("authenticationToken")] public bool Active { get; set; }
}

public sealed class VTSAPIErrorResponse : VTSResponse<VTSAPIErrorResponseData>;
public sealed class VTSAPIErrorResponseData : VTSPacketData
{
    [JsonPropertyName("errorID")] public long ErrorID { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

public sealed class VTSAuthenticationRequest : VTSRequest<VTSAuthenticationRequestData>
{
    public override string? MessageType { get; set; } = "AuthenticationRequest";
}
public sealed class VTSAuthenticationRequestData : VTSPacketData
{
    [JsonPropertyName("pluginName")] public required string? PluginName { get; set; }
    [JsonPropertyName("pluginDeveloper")] public required string? PluginDeveloper { get; set; }
    [JsonPropertyName("authenticationToken")] public required string? AuthenticationToken { get; set; }
}

public sealed class VTSAuthenticationResponse : VTSResponse<VTSAuthenticationResponseData>;
public sealed class VTSAuthenticationResponseData : VTSPacketData
{
    [JsonPropertyName("authenticated")] public required string? Authenticated { get; set; }
    [JsonPropertyName("reason")] public required string? Reason { get; set; }
}

public sealed partial class VTubeStudio : ObservableObject
{
    public const ushort DefaultPort = 8001;

    public static readonly VTubeStudio Instance = new();

    [ObservableProperty] public partial ushort Port { get; set; } = DefaultPort;
    [ObservableProperty] public partial ConnectionStatus Status { get; private set; }
    [ObservableProperty] public partial string AccessToken { get; private set; } = string.Empty;

    CancellationTokenSource? Identity;

    [RelayCommand] public void Start() => Application.Current.Dispatcher.Invoke(StartImmediate);
    public void StartImmediate()
    {
        if (Status != ConnectionStatus.Offline) return;
        try
        {
            Communication(Identity = new());
            Status = ConnectionStatus.Pending;
            $"{this} Started.".Out();
        }
        catch (Exception ex)
        {
            ex.Out();
            Status = ConnectionStatus.Offline;
        }
    }

    [RelayCommand] public void Stop() => Application.Current.Dispatcher.Invoke(StopImmediate);
    public void StopImmediate()
    {
        if (Status == ConnectionStatus.Offline) return;
        try
        {
            if (Identity is not null)
            {
                if (!Identity.IsCancellationRequested)
                    Identity.Cancel();
                Identity = null;
            }
            Status = ConnectionStatus.Offline;
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
            token.Register(() => PortSource.TrySetCanceled());
            await PortSource.Task;
            VTubeStudioDiscovery.Instance.Release(this);

            // Constructs Port.
            if (identity.IsCancellationRequested) return;
            if (PortSource.Task.IsCanceled) return;
            Uri uri = new($"ws://localhost:{PortSource.Task.Result}");
            $"{this} URI Constructed successfully: {uri}".Out();

            // Reads app icon as Base64 to use as plugin preview.
            string image = string.Empty;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(path))
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);
                image = Convert.ToBase64String(bytes);
            }

            // Establishes connection with a server.
            socket = new VTSSocket(identity, new());
            await socket.ConnectAsync(uri, token);
            ReportStatus(identity, ConnectionStatus.Online);
            $"{this} Successfully connected to VTuber Studio!".Out();

            // Initial connection.
            {
                const string RequestID = "ConnectionRequest";
                // Receive command should always be initialized first.
                var response = socket.ReceiveAsync<VTSAPIStateResponse>();
                await socket.SendAsync(new VTSAPIStateRequestData { RequestID = RequestID });
                var packet = await response;
                $"Received {RequestID} result:\n{packet}".Out(ConsoleColor.Cyan);
            }

            // Authentication.
        }
        catch (Exception ex)
        {
            // TODO: Toast failure on UI.
            //  Propt them to enable API access.
            ex.Out(ToString());
            identity.Cancel();
        }
        finally
        {
            VTubeStudioDiscovery.Instance.Release(this);
            ReportStatus(identity, ConnectionStatus.Offline);
            socket?.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReportStatus(CancellationTokenSource identity, ConnectionStatus status)
    {
        Application.Current.Dispatcher.Invoke(() => ReportStatusImmediate(identity, status));
    }
    private void ReportStatusImmediate(CancellationTokenSource identity, ConnectionStatus status)
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

public sealed class VTSSocket : IDisposable
{
    const int InitialBufferSize = 1024 * 64;
    const int MaxBufferSize = 1024 * 1024 * 16;
    public delegate void StartHandler(VTSSocket client);
    public delegate void ReceiveHandler(VTSSocket client, ReadOnlySpan<char> message);
    public delegate void StopHandler(VTSSocket client);
    public readonly CancellationTokenSource Identity;
    public readonly CancellationToken Token;
    public readonly ClientWebSocket Socket;
    public readonly Encoding Encoding;

    byte[] Buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
    readonly Mutex ReceiveMutex = new();
    readonly Mutex SendMutex = new();
    readonly Lock Lock = new();
    volatile bool IsStarted;
    volatile bool Disposed;

    public VTSSocket(CancellationTokenSource identity, ClientWebSocket socket, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(Identity = identity);
        ArgumentNullException.ThrowIfNull(Socket = socket);
        Encoding = encoding ?? Encoding.UTF8;
        Token = Identity.Token;
    }

    public Task ConnectAsync(Uri uri, CancellationToken token)
    {
        using (Lock.EnterScope())
        {
            Token.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(Disposed, this);
            AlreadyStartedException.ThrowIf(IsStarted, this);
            IsStarted = true;
        }

        return Socket.ConnectAsync(uri, token);
    }

    public ValueTask SendAsync<T>(T obj)
    {
        string json = JsonSerializer.Serialize(obj, VTSPackets.JsonOptions);
        return SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true);
    }
    public ValueTask SendAsync(ReadOnlySpan<byte> bytes, WebSocketMessageType type, bool endOfMessage)
    {
        using (Lock.EnterScope())
        {
            Token.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(Disposed, this);
            NotStartedException.ThrowIf(!IsStarted, this);
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
        bytes.CopyTo(buffer);
        return SendAsyncCore(buffer, bytes.Length, type, endOfMessage);
    }

    async ValueTask SendAsyncCore(byte[] bytes, int length, WebSocketMessageType type, bool endOfMessage)
    {
        SendMutex.WaitOne();
        try
        {
            await Socket.SendAsync(new Memory<byte>(bytes, 0, length), type, endOfMessage, Token);
        }
        catch (Exception ex) { ex.Out(); }
        finally
        {
            SendMutex.ReleaseMutex();
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    public readonly struct ReceiveResult(bool success, char[]? buffer, int length) : IDisposable
    {
        public static readonly ReceiveResult Faulted = new(false, null, 0);
        public readonly bool Success = success;
        public Memory<char> Message => (buffer ?? throw new InvalidOperationException("Result doesn't contant a message.")).AsMemory(0, length);
        public void Dispose()
        {
            if (buffer is not null) ArrayPool<char>.Shared.Return(buffer);
        }
    }

    public async Task<T?> ReceiveAsync<T>()
    {
        ReceiveResult result = await ReceiveAsync();
        if (!result.Success)
        {
            $"Failed to receive ({typeof(T)}) from the server!".Out();
        }

        try
        {
            return JsonSerializer.Deserialize<T>(result.Message.Span, VTSPackets.JsonOptions);
        }
        catch (JsonException)
        {
            $"Failed to deserialize {typeof(T)} from data:\n{new string(result.Message.Span)}".Out();
        }
        finally
        {
            result.Dispose();
        }

        return default;
    }

    public async Task<ReceiveResult> ReceiveAsync()
    {
        using (Lock.EnterScope())
        {
            Token.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(Disposed, this);
            NotStartedException.ThrowIf(!IsStarted, this);
        }

        ReceiveMutex.WaitOne();
        char[]? chars = null;
        int head = 0;
        try
        {
            while (!Token.IsCancellationRequested)
            {
                var result = await Socket.ReceiveAsync(new(Buffer, head, Buffer.Length - head), Token);
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", Token);
                        $"{this} VTube Studio gracefully closed connection.".Out(ConsoleColor.Yellow);
                        return ReceiveResult.Faulted;

                    case WebSocketMessageType.Binary: $"{this} Reading binary is not supported.".Out(ConsoleColor.Yellow); break;
                    case WebSocketMessageType.Text when !result.EndOfMessage:
                        head += result.Count;
                        if (head < Buffer.Length) break;

                        int newLength = checked(Buffer.Length * 2);
                        if (newLength > MaxBufferSize)
                        {
                            $"Message is too big! (over {head}/{MaxBufferSize} bytes)".Out();
                            await Socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "", Token);
                            return ReceiveResult.Faulted;
                        }

                        var temp = ArrayPool<byte>.Shared.Rent(newLength);
                        System.Buffer.BlockCopy(Buffer, 0, temp, 0, Buffer.Length);
                        ArrayPool<byte>.Shared.Return(Buffer);
                        Buffer = temp;
                        break;

                    case WebSocketMessageType.Text:
                        int length = checked(head + result.Count);
                        head = 0;

                        var bytes = Buffer.AsSpan(0, length);
                        chars = ArrayPool<char>.Shared.Rent(Encoding.GetCharCount(bytes));
                        int total = Encoding.GetChars(bytes, chars);
                        ReceiveResult ret = new(true, chars, total);
                        chars = null;
                        return ret;
                }
            }
        }
        finally
        {
            if (chars is not null)
                ArrayPool<char>.Shared.Return(chars);
        }

        return ReceiveResult.Faulted;
    }

    public void Dispose()
    {
        using (Lock.EnterScope())
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            Disposed = true;
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }

    public override string ToString() => $"[{nameof(VTSSocket)}]";
}

public sealed class NotStartedException(string message) : Exception(message)
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowIf(bool state, object? target)
    {
        if (state) throw new NotStartedException($"{target} Was not started!");
    }
}

public sealed class AlreadyStartedException(string message) : Exception(message)
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowIf(bool state, object? target)
    {
        if (state) throw new AlreadyStartedException($"{target} Is already started!");
    }
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