using CommunityToolkit.Mvvm.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

public sealed partial class VTubeStudioDiscovery : ObservableObject
{
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
        using UdpClient client = new(port);
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
