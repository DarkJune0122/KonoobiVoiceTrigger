using CommunityToolkit.Mvvm.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

/// <summary>
/// Describes VTubeStudio instance.
/// </summary>
public sealed partial class VTSEndPoint : ObservableObject
{
    /// <summary>
    /// Whether VTubeStudio can be found in the up-stream discovery data.
    /// </summary>
    /// <remarks>
    /// If this value is ever set to <see langword="false"/> - you can consider end-point to be invalid.
    /// </remarks>
    [ObservableProperty] public partial bool Observed { get; set; }
    /// <summary>
    /// Whether VTube Studio application instance is active.
    /// Assumption: Plugins will not be able to connect while instance is inactive.
    /// This can indicate API access setting being disabled in the settings.
    /// </summary>
    [ObservableProperty] public partial bool Active { get; set; }
    [ObservableProperty] public partial ushort Port { get; set; }
    [ObservableProperty] public partial string InstanceID { get; set; }
    [ObservableProperty] public partial string WindowTitle { get; set; }
}
/*
"active": false,
"port": 8001,
"instanceID": "93aa0d0494304fddb057ae8a295c4e59",
"windowTitle": "VTube Studio"
*/

public sealed partial class VTSConnection : ObservableObject, IDisposable
{
    [ObservableProperty] public partial VTSEndPoint EndPoint { get; private set; }
    [ObservableProperty] public partial VSTPlugin Plugin { get; private set; }
    [ObservableProperty] public partial VTSStatus Status { get; set; }
    [ObservableProperty] public partial bool Authenticated { get; set; }

    CancellationToken Token;

    /// <summary>
    /// Collects provided 
    /// </summary>
    /// <param name="plugin"></param>
    public async Task<bool> ConnectAsync(VTSEndPoint VTubeStudioPlugin plugin, CancellationToken token)
    {
        
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public override string ToString() => $"[{nameof(VTSConnection)}]";
}

public sealed partial class VTSDiscovery : ObservableObject
{
    public enum Status : byte
    {
        Inactive,
        Active,
    }

    public static readonly VTSDiscovery Instance = new();

    public delegate void UpdateEventHandler(VTSDiscovery service);

    public event UpdateEventHandler? OnInformationUpdated;

    // Note: what to do with UI callbacks? How to syncronize them with UI?
    //  Maybe we can run syncronizaation code on VM interfacing VTSDiscovery system?
    [ObservableProperty] public partial bool Active { get; set; }
    [ObservableProperty] public partial ushort Port { get; set; } = 47779;
    [ObservableProperty] public partial double RestartDelay { get; set; } = 2;

    CancellationTokenSource? Identity;
    readonly Lock Lock = new();

    public void Start()
    {
        lock (Lock)
        {
            if (Active)
            {
                $"{this} Discovery system is already started.".Out(ConsoleColor.Yellow);
                return;
            }

            $"{this} Starting VTubeStudio discovery system...".Out();
            try
            {
                $"{this} Started successfully.".Out();
                return;
            }
            catch (Exception ex) { ex.Out($"{this} Failed to start! Reattempting in ({RestartDelay}) seconds."); }
            try
            {
                Task.Delay(TimeSpan.FromSeconds(RestartDelay))
                    .ContinueWith(static async (state) => , this);
            }
            catch (Exception ex)
            {
                ex.Out($"{this} Failed to schedule a restart! VTubeStudio integration will not restart this session!");
            }
        }
    }

    async ValueTask Restart()
    {

    }

    public void Stop()
    {

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

    public override string ToString() => $"[{nameof(VTSDiscovery)}]";
}
