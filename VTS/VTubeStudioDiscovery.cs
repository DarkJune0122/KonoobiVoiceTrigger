using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

public sealed partial class ConcurrentTokenStorage : ObservableObject
{
    /// <summary>
    /// Directory to create, at which token will be stored.
    /// </summary>
    public string FileDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory;
    /// <summary>
    /// Path where file exists.
    /// </summary>
    public string FilePath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vts-auth");

    // Regular semaphore, as it is used for I/O operations.
    // I/O operations might take a while, due to hardware stalls, such as user having to spin-up the HDD for a few seconds.
    // We would have used SemaphoreSlim if waiting times were expected to be very short, where spin-locking is an option.
    // More info: https://learn.microsoft.com/en-us/dotnet/standard/threading/semaphore-and-semaphoreslim
    readonly Semaphore Semaphore = new(0, 1);
    Task<string?>? IOTask = null;

    /// <returns>
    /// A non-null string when token is found.
    /// <see langword="null"/> if token is invalidated, or cannot be loaded.
    /// </returns>
    public async ValueTask<string?> GetToken()
    {
        await Semaphore..WaitAsync();
        try
        {
            return await (IOTask ??= ReaderTask());
        }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { Semaphore.Release(); }
        return null;
    }

    async Task<string?> ReaderTask()
    {
        if (!File.Exists(FilePath))
            return null;

        string token = await File.ReadAllTextAsync(FilePath);
        if (string.IsNullOrEmpty(token))
            return null;

        return token;
    }

    public async ValueTask SetToken(string token)
    {
        await Semaphore.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(FilePath, token);
        }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { Semaphore.Release(); }
    }

    public async ValueTask DeleteToken()
    {
        await Semaphore.WaitAsync();
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { Semaphore.Release(); }
    }

    public override string ToString() => $"[{nameof(ConcurrentTokenStorage)}]";
}

public sealed partial class VTubeStudioEndPoint : ObservableObject
{

}

public sealed partial class VTubeStudioConnection : ObservableObject
{
    [ObservableProperty] public partial VTSStatus Status { get; set; }
    /// <summary>
    /// Whether VTube Studio application instance is active.
    /// Assumption: Plugins will not be able to connect while instance is inactive.
    /// This can indicate API access setting being disabled in the settings.
    /// </summary>
    [ObservableProperty] public partial bool Active { get; set; }

    [ObservableProperty] public partial bool Authenticated { get; set; }
}

public sealed partial class VTubeStudioDiscovery : ObservableObject
{
    public enum Status : byte
    {
        Inactive,
        Active,
    }

    public static readonly VTubeStudioDiscovery Instance = new();

    public delegate void UpdateEventHandler(VTubeStudioDiscovery service);

    public event UpdateEventHandler? OnInformationUpdated;

    [ObservableProperty] public partial VTSStatus Status { get; private set; }
    [ObservableProperty] public partial ushort Port { get; set; } = 47779;
    [ObservableProperty] public partial bool VTSActive { get; private set; } = false;
    [ObservableProperty] public partial ushort VTSPort { get; private set; } = 0;
    [ObservableProperty] public partial string VTSInstanceID { get; private set; } = string.Empty;
    [ObservableProperty] public partial string VTSWindowTitle { get; private set; } = string.Empty;

    readonly HashSet<object> Requests = [];
    CancellationTokenSource? Identity;

    public readonly struct Scope(VTubeStudioDiscovery service, object user) : IDisposable
    {
        public void Dispose() => service.Release(user);
    }

    public Scope RequestScope(object user)
    {
        Request(user);
        return new(this, user);
    }
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
        if (Identity is not null) return;
        try
        {
            Communication(Port, Identity = new());
            Status = VTSStatus.Pending;
            $"{this} Started.".Out();
        }
        catch (Exception ex) { ex.Out(ToString()); }
    }

    //[RelayCommand] public void Stop() => Application.Current.Dispatcher.Invoke(StopImmediate);
    private void StopImmediate()
    {
        if (Identity is null) return;
        try
        {
            Identity.Cancel();
            Identity = null;
            Status = VTSStatus.Offline;
            // Resets all the info, to force requestors to restart the discovery system.
            VTSActive = default;
            VTSPort = default;
            VTSInstanceID = string.Empty;
            VTSWindowTitle = string.Empty;
            $"{this} Stopped.".Out();
        }
        catch (Exception ex) { ex.Out(ToString()); }
    }

    private async void Communication(ushort port, CancellationTokenSource identity)
    {
        CancellationToken token = identity.Token;
        UdpClient? client = null;
        try
        {
            client = new();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //client.EnableBroadcast = true; // Option for sending - not receiving broadcasts.
            client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        }
        catch (Exception ex)
        {
            ex.Out($"{this} Cannot start listening to the VTubeStudio precense signals!\n");
            client?.Dispose();
            throw;
        }

        // Micro-delay to ensure the status have changed.
        try
        {
            await Task.Delay(1);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await client.ReceiveAsync(token);
                    ReportStatus(identity, VTSStatus.Online);

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
                }
                catch (Exception ex)
                {
                    ReportStatus(identity, VTSStatus.Pending);
                    ex.Out(ToString());
                    await Task.Delay(2000);
                    $"{this} Restarting...".Out();
                }
                await Task.Delay(300);
            }

            ReportStatus(identity, VTSStatus.Offline);
        }
        finally
        {
            client?.Dispose();
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

    public override string ToString() => $"[{nameof(VTubeStudioDiscovery)}]";
}
