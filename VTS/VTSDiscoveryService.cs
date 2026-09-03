using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using VoiceTrigger.Logging;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

/// <summary>
/// Responsible for discovering new instances of VTube Studio and timing out old ones based on keep-alive events.
/// Instances are provided as a list o <see cref="VTSEndPoint"/>s, to which <see cref="VTSConnection"/> can connect to.
/// </summary>
/// <remarks>
/// Disabling this service mid-program-execution will lead to <see cref="VTSEndPoint"/>s
/// only being killed by <see cref="VTSConnection"/> on some critical exceptions (no examples yet),
/// or only explicitly from user/developer input, by calling <see cref="VTSEndPoint.Kill"/>.
/// </remarks>
/// Task:
/// 1. Start on Start.
/// 2. Stop on Stop.
/// 3. Automatically restart if failed.
/// 4. Update list of valid end-points.
/// 5. Invalidate existing end-points.
/// 6. Do NOT dispatch events to UI thread (reduces other thread's waiting period).
public sealed partial class VTSDiscoveryService : ObservableObject, IService
{
    public const ushort DefaultVTubeStudioDiscoveryPort = 47779;
    public const double DefaultRestartDelay = 2;
    public const double DefaultKeepAliveCheckInterval = 5;
    public const double DefaultEndPointMaxKeepAliveInterval = 60;
    public const long MinimumEndPointMaxKeepAliveIntervalMs = 250;

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

    public event ActiveChangedEventHandler? ActiveChanged;

    public ObservableCollection<VTSEndPoint> EndPoints { get; } = [];
    public bool Active
    {
        get => field;
        private set
        {
            lock (Lock)
            {
                if (field != value)
                {
                    OnPropertyChanging(KnownEventArgs.ActiveChanging);
                    field = value;
                    ActiveChanged?.Invoke(value);
                    OnPropertyChanged(KnownEventArgs.ActiveChanged);
                }
            }
        }
    }
    /// <summary>
    /// Only updated during <see cref="Initialize"/>.
    /// </summary>
    public static ushort Port
    {
        get => Roaming.VTubeStudioDiscoveryPort;
        set => Roaming.VTubeStudioDiscoveryPort = value;
    }
    public static double RestartDelay
    {
        get => Roaming.VTubeStudioDiscoveryRestartDelay;
        set => Roaming.VTubeStudioDiscoveryRestartDelay = value;
    }
    public static double KeepAliveCheckInterval
    {
        get => Roaming.KeepAliveCheckInterval;
        set => Roaming.KeepAliveCheckInterval = value;
    }
    public static double EndPointMaxKeepAliveInterval
    {
        get => Roaming.EndPointMaxKeepAliveInterval;
        set => Roaming.EndPointMaxKeepAliveInterval = value;
    }

    static int WorkerCounter;
    readonly Lock Lock = new();
    CancellationTokenSource? Identity;

    /// <inheritdoc/>
    public void Initialize() => Start();

    /// <inheritdoc/>
    public void Terminate() => Stop();

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

            case StartResult.AlreadyStarted: // Replace with exception?
                $"{this} Discovery system is already started.".Out(ConsoleColor.Yellow);
                break;

            case StartResult.Success: break;
            default: throw new SwitchExpressionException(result);
        }
    }

    /// <summary>
    /// Tries to stop the system.
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

            case StopResult.AlreadyStopped: // Replace with exception?
                $"{this} Discovery system is already stopped.".Out(ConsoleColor.Yellow);
                break;

            case StopResult.Success: break;
            default: throw new SwitchExpressionException(result);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long NextWorkerCounter() => Interlocked.Increment(ref WorkerCounter);
    StartResult StartInternal()
    {
        Lock.Enter();
        CancellationTokenSource? identity = null;
        if (Identity is not null)
        {
            Lock.Exit(); // Quick lock, without initializing a try block.
            return StartResult.AlreadyStarted;
        }
        try // Try block is required if any activation events throw an exception.
        {
            Active = true; // Active is set first, to avoid allocating an identiy class on exception.
            identity = new();
            Identity = identity;
            string id = NextWorkerCounter().ToString();
            ReaderWorker(identity, id);
            TimeoutWorker(identity, id);
            return StartResult.Success;
        }
        catch (Exception ex)
        {
            ex.Out(this);
            try { Identity = null; identity?.Cancel(); Active = false; }
            catch (Exception ex2) { ex2.Out($"{this} Exception while reverting state in a start method!\n"); }
            return StartResult.Failed;
        }
        finally { Lock.Exit(); }
    }

    private async void TimeoutWorker(CancellationTokenSource identity, string id)
    {
        $"{this} Timeout worker #{id} started.".Out(ConsoleColor.Gray);
        await Task.Yield(); // Makes sure that all service states are initialized before worker actually starts.
        CancellationToken token = identity.Token;
        List<VTSEndPoint> endPoints = [];
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                long maxAllowed = Math.Max(
                    val1: MinimumEndPointMaxKeepAliveIntervalMs,
                    val2: (long)TimeSpan.FromSeconds(EndPointMaxKeepAliveInterval).TotalMilliseconds);
                lock (Lock)
                {
                    long tick = Environment.TickCount64;
                    foreach (var ep in EndPoints)
                    {
                        long delta = tick - ep.LastKeepAliveTick;
                        if (delta > maxAllowed) endPoints.Add(ep);
                    }

                    foreach (var ep in endPoints)
                    {
                        try { EndPoints.Remove(ep); }
                        catch (Exception ex) { ex.Out($"{this} VTSEndPoint removal exception!\n"); }
                        ep.Kill();
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(this); }
            finally { endPoints.Clear(); }
        }
        lock (Lock)
        {
            if (Identity == identity)
            {
                Identity = null;
                try { identity.Cancel(); }
                catch (Exception ex) { ex.Out($"{this} Exception during identity cancellation of a timeout worker #{id}!\n"); }
                try { Active = false; }
                catch (Exception ex) { ex.Out($"{this} Exception during identity deactivation of a timeout worker #{id}!\n"); }
            }
        }
        $"{this} Timeout worker #{id} quit.".Out(ConsoleColor.Gray);
    }

    private async void ReaderWorker(CancellationTokenSource identity, string id)
    {
        $"{this} Starting communication worker #{id}...".Out(ConsoleColor.Gray);
        await Task.Yield(); // Makes sure that all service states are initialized before worker actually starts.
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

                $"{this} Discovery worker #{id} started on port {Port}.".Out(ConsoleColor.Gray);
                while (!token.IsCancellationRequested)
                {
                    // TODO: UdpClient allocates an array on each receive.
                    //  Optimize by making a custom UdpSocket implementation.
                    var result = await client.ReceiveAsync(token);
                    // TODO: Report status as 'Online' when received any data for the first time.

                    if (result.Buffer.Length == 0) continue;
                    VTSDiscoveryResponse? packet;
                    VTSDiscoveryResponseData data;
                    try
                    {
                        packet = JsonSerializer.Deserialize<VTSDiscoveryResponse>(result.Buffer, VTSPackets.JsonOptions);
                        if (packet is null || packet.Data is null)
                        {
                            $"{this} Deserialized discovery data as 'null'. Packet will be ignored.".Out(ConsoleColor.Gray);
                            if (Roaming.LogNetworkPackets)
                            {
                                if (packet is null) $"{this} Packet: null".Out(ConsoleColor.Gray);
                                else $"{this} Packet:\n{packet}".Out(ConsoleColor.Gray);
                            }
                            continue;
                        }
                        data = packet.Data;
                    }
                    catch (Exception ex)
                    {
                        ex.Out($"{this} Discovery data deserialization exception! Packet will be ignored.\n");
                        if (Roaming.LogNetworkPackets)
                        {
                            $"{this} Packet:\n{Encoding.UTF8.GetString(result.Buffer)}".Out(ConsoleColor.Gray);
                        }
                        continue;
                    }

                    $"{this} Received discovery packet.".Out(ConsoleColor.Gray);
                    if (Roaming.LogNetworkPackets)
                    {
                        if (packet is null) $"{this} Packet: null".Out(ConsoleColor.Gray);
                        else $"{this} Packet:\n{packet}".Out(ConsoleColor.Gray);
                    }

                    // Note: if this will cause large stalls - fix it.
                    lock (Lock)
                    {
                        token.ThrowIfCancellationRequested();
                        var match = EndPoints.FirstOrDefault(e => e.InstanceID == data.InstanceID);
                        if (match is not null)
                        {
                            if (match.Port != data.Port)
                            {
                                $"{this} Detected port change in remote VTude Studio #{match.InstanceID}. Active connections will be killed and restarted.".Out(ConsoleColor.Yellow);
                                try { EndPoints.Remove(match); }
                                catch (Exception ex) { ex.Out($"{this} VTSEndPoint removal exception!\n"); }
                                match.Kill();
                            }
                            else
                            {
                                match.KeepAlive();
                                match.Active = data.Active;
                                match.WindowTitle = data.WindowTitle ?? string.Empty;
                            }
                        }
                        else
                        {
                            var ep = new VTSEndPoint
                            {
                                Active = data.Active,
                                Port = data.Port,
                                InstanceID = data.InstanceID ?? string.Empty,
                                WindowTitle = data.WindowTitle ?? string.Empty,
                            };
                            $"{this} Discovered new VTube Studio instance! #{ep.InstanceID}".Out(ConsoleColor.Gray);
                            try { EndPoints.Add(ep); } // Does not revert on exception, in case the refernce is still valid to use.
                            catch (Exception ex) { ex.Out($"{this} VTSEndPoint addition exception!\n"); }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(this); }
            finally { client?.Dispose(); }
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(RestartDelay), token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { ex.Out(this); break; }
            $"{this} Restarting discovery worker #{id}...".Out(ConsoleColor.Gray);
        }
        lock (Lock)
        {
            if (Identity == identity)
            {
                Identity = null;
                try { identity.Cancel(); }
                catch (Exception ex) { ex.Out($"{this} Exception during identity cancellation of a discovery worker #{id}!\n"); }
                try { Active = false; }
                catch (Exception ex) { ex.Out($"{this} Exception during identity deactivation of a discovery worker #{id}!\n"); }
            }
        }
        $"{this} Discovery worker #{id} quit.".Out(ConsoleColor.Gray);
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

    public override string ToString() => $"[{nameof(VTSDiscoveryService)}]";

    static class KnownEventArgs
    {
        public static readonly PropertyChangingEventArgs ActiveChanging = new(nameof(Active));
        public static readonly PropertyChangedEventArgs ActiveChanged = new(nameof(Active));
    }
}
