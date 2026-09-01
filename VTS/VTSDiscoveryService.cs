using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
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

/// <remarks>
/// All events are NOT UI-thread safe!
/// </remarks>
/// Task:
/// 1. Start on Start.
/// 2. Stop on Stop.
/// 3. Automatically restart if failed.
/// 4. Update list of valid end-points.
/// 5. Invalidate existing end-points.
/// 6. Do NOT dispatch events to UI thread (reduces other thread's waiting period).
public sealed class VTSDiscoveryService : IService
{
    // This value never changes, so we will not implement a property for it.
    public const ushort KnownVTubeStudioDiscoveryPort = 47779;

    enum StartResult : byte
    {
        Failed,
        AlreadyStarted,
        Success,
    }

    enum StopResult : byte
    {
        Failed,
        AlreadyStopped,
        Success,
    }

    public static readonly VTSDiscoveryService Instance = new();

    public delegate void ActiveChangedEventHandler(bool active);

    public event ActiveChangedEventHandler? OnActiveChanged;

    public ObservableCollection<VTSEndPoint> EndPoints { get; } = [];

    // Note: callback syncronization with UI happens on VM level.
    public bool Active
    {
        get => field;
        private set
        {
            lock (Lock)
            {
                if (field == value) return;
                field = value;
                OnActiveChanged?.Invoke(value);
            }
        }
    }
    /// <summary>
    /// Only updated during <see cref="Initialize"/>.
    /// </summary>
    public ushort Port { get; private set; } = KnownVTubeStudioDiscoveryPort;
    public double RestartDelay
    {
        get => Interlocked.CompareExchange(ref field, 0, 1); // Returns current value, without changing it.
        private set => Interlocked.Exchange(ref field, value);
    } = 2;

    static int WorkerCounter;
    readonly Lock Lock = new();
    CancellationTokenSource? Identity;

    /// <inheritdoc/>
    public void Initialize() => Port = Roaming.VTubeStudioDiscoveryPort;

    /// <inheritdoc/>
    public void Terminate() { }

    /// <summary>
    /// Stops system if it is active, and starts it again.
    /// </summary>
    /// <remarks>
    /// Save to call even if system is not active.
    /// </remarks>
    public void Restart()
    {
        TryStop();
        TryStart();
    }

    /// <summary>
    /// Tries to start the system.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Start"/>, doesn't log anything to console if system is already started.
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> - if started successfully.
    /// <see langword="false"/> - if starting caused an exception. See logs for more info.
    /// </returns>
    public bool TryStart()
    {
        StartResult startResult = StartInternal();
        switch (startResult)
        {
            case StartResult.Failed:
                $"{this} Failed to start VTubeStudio discovery system!".Out(ConsoleColor.Red);
                return false;

            case StartResult.AlreadyStarted:
            case StartResult.Success: return true;
            default: throw new SwitchExpressionException(startResult);
        }
    }

    public void Start()
    {
        var result = StartInternal();
        switch (result)
        {
            case StartResult.Failed:
                $"{this} Failed to start VTubeStudio discovery system!".Out(ConsoleColor.Red);
                break;

            case StartResult.AlreadyStarted:
                $"{this} Discovery system is already started.".Out(ConsoleColor.Yellow);
                break;

            case StartResult.Success: break;
            default: throw new SwitchExpressionException(result);
        }
    }

    static long NextWorkerCounter() => Interlocked.Increment(ref WorkerCounter);
    StartResult StartInternal()
    {
        Lock.Enter();
        if (Identity is not null)
        {
            Lock.Exit(); // Quick lock, without initializing a try block.
            return StartResult.AlreadyStarted;
        }
        try // Try block is required if any activation events throw an exception.
        {
            Active = true; // Active is set first, to avoid allocating an identiy class on exception.
            CancellationTokenSource? identity = new();
            Identity = identity;
            Worker(identity, NextWorkerCounter().ToString());
            return StartResult.Success;
        }
        catch (Exception ex)
        {
            ex.Out(this);
            try { Identity = null; Active = false; }
            catch (Exception ex2) { ex2.Out($"{this} Exception while reverting state in a start method!\n"); }
            return StartResult.Failed;
        }
        finally { Lock.Exit(); }
    }

    async void Worker(CancellationTokenSource identity, string id)
    {
        $"[{this}] Starting worker #{id}...".Out(ConsoleColor.Gray);
        CancellationToken token = identity.Token;
        while (!token.IsCancellationRequested)
        {
            UdpClient? client = null;
            try
            {
                client = new();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //client.EnableBroadcast = true; // Option for sending - not receiving broadcasts.
                client.Client.Bind(new IPEndPoint(IPAddress.Any, Port));

                while (!token.IsCancellationRequested)
                {
                    // TODO: UdpClient allocates an array on each receive.
                    //  Optimize by making a custom UdpSocket implementation.
                    var result = await client.ReceiveAsync(token);
                    // TODO: Report status as 'Online' when received any data for the first time.

                    if (result.Buffer.Length == 0) continue;
                    VTSDiscoveryResponseData data;
                    try
                    {
                        var packet = JsonSerializer.Deserialize<VTSDiscoveryResponse>(result.Buffer);
                        if (packet is null)
                        {
                            $"{this} Deserialized discovery data sa 'null'. Packet will be ignored.".Out(ConsoleColor.Gray);
                            if (Roaming.LogNetworkPackets)
                            {
                                $"{this} Packet:\n{packet}".Out(ConsoleColor.Gray);
                            }
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.Out($"{this} Discovery data deserialization exception! Packet will be ignored.\n");
                        if (Roaming.LogNetworkPackets)
                            continue;
                    }

                    var data = JsonSerializer.Deserialize<VTSDiscoveryResponse>(result.Buffer)?.Data;
                    if (data is null)
                    {
                        $"{this} Cannot deserialize VTSDiscoveryResponse data.".Out(ConsoleColor.Red);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(this); break; }
            finally { client?.Dispose(); }
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(RestartDelay), token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(this); break; }
            $"{this} Restarting worker #{id}...".Out(ConsoleColor.Gray);
        }
        lock (Lock)
        {
            if (Identity == identity)
            {
                Identity = null;
                try { Active = false; }
                catch (Exception ex) { ex.Out($"{this} Exception during service state reset in worker #{id}!\n"); }
            }
        }
        $"{this} Worker #{id} quit.".Out(ConsoleColor.Gray);
    }

    /// <summary>
    /// Troes to stop the system.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Stop"/>, doesn't log anything to console if system is already stopped.
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> - if stoppped successfully.
    /// <see langword="false"/> - if stopping caused an exception. See logs for more info.
    /// </returns>
    public bool TryStop()
    {
        StopResult stopResult = StopInternal();
        switch (stopResult)
        {
            case StopResult.Failed:
                $"{this} Failed to stop VTubeStudio discovery system!".Out(ConsoleColor.Red);
                return false;

            case StopResult.AlreadyStopped:
            case StopResult.Success: return true;
            default: throw new SwitchExpressionException(stopResult);
        }
    }

    public void Stop()
    {
        StopResult result = StopInternal();
        switch (result)
        {
            case StopResult.Failed:
                $"{this} Failed to stop VTubeStudio discovery system!".Out(ConsoleColor.Red);
                break;

            case StopResult.AlreadyStopped:
                $"{this} Discovery system is already stopped.".Out(ConsoleColor.Yellow);
                break;

            case StopResult.Success: break;
            default: throw new SwitchExpressionException(result);
        }
    }

    StopResult StopInternal()
    {
        Lock.Enter();
        if (Identity is null)
        {
            Lock.Exit();
            return StopResult.AlreadyStopped;
        }
        try
        {
            Identity.Cancel();
            Identity = null;
            Active = false; // Might throw - executing last.
            return StopResult.Success;
        }
        catch (Exception ex)
        {
            ex.Out(this);
            Identity = null;
            try { Active = false; }
            catch (Exception ex2) { ex2.Out($"{this} Exception while resetting state in a stop method!\n"); }
            return StopResult.Failed;
        }
        finally { Lock.Exit(); }
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

    public override string ToString() => $"[{nameof(VTSDiscoveryService)}]";
}
