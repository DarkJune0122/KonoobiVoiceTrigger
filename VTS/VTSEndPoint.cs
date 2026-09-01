using VoiceTrigger.Logging;

namespace VoiceTrigger.VTS;

/// <summary>
/// Describes VTubeStudio instance.
/// </summary>
public sealed class VTSEndPoint
{
    public delegate void ActiveChangedEventHandler(bool active);
    public delegate void WindowTitleChangedEventHandler(string title);

    public event ActiveChangedEventHandler? OnActiveChanged;
    public event WindowTitleChangedEventHandler? OnWindowTitleChanged;

    /// <summary>
    /// Whether VTubeStudio can be found in the up-stream discovery data.
    /// </summary>
    /// <remarks>
    /// If this value is ever set to <see langword="false"/> - end-point is considered to be invalid,
    /// and will never be modified by <see cref="VTSDiscoveryService"/> again.
    /// </remarks>
    public bool Alive => !CancellationSource.IsCancellationRequested;
    /// <summary>
    /// <see cref="CancellationTokenSource"/> linked to <see cref="Alive"/> parameter.
    /// Cancelled whenever anything uses <see cref="Kill"/> on a thread that called <see cref="Kill"/>.
    /// </summary>
    public CancellationTokenSource CancellationSource { get; } = new();
    /// <summary>
    /// Last tick when a keep alive event occured.
    /// </summary>
    public long LastKeepAliveTick
    {
        get => Interlocked.Read(ref field);
        private set => Interlocked.Exchange(ref field, value);
    } = Environment.TickCount64;

    /// <summary>
    /// Instance ID of a unique VTube Studio application.
    /// </summary>
    /// <remarks>
    /// When user changes port in VTS API settings - plugin will be disconnected,
    /// but InstanceID will remain the same after a reconnect.
    /// </remarks>
    public required string InstanceID { get; init; }
    /// <summary>
    /// Port at which VTube Studio is running.
    /// </summary>
    /// <remarks>
    /// <see cref="VTSDiscoveryService"/> will kill this <see cref="VTSEndPoint"/> with <see cref="Kill"/>
    /// if it ever detects that the same <see cref="InstanceID"/> started to use a different <see cref="Port"/>.
    /// </remarks>
    public required ushort Port { get; init; }
    /// <summary>
    /// Whether VTube Studio application instance is active.
    /// Assumption: Plugins will not be able to connect while instance is inactive.
    /// This can indicate API access setting being disabled in the settings.
    /// </summary>
    public required bool Active
    {
        get
        {
            lock (Lock) return field;
        }
        set
        {
            lock (Lock)
            {
                if (field != value)
                {
                    Interlocked.Exchange(ref field, value);
                    OnActiveChanged?.Invoke(value);
                }
            }
        }
    }
    /// <summary>
    /// Title of the window.
    /// </summary>
    /// <remarks>
    /// In v1.0: Dynamically changes on each discovery request.
    /// </remarks>
    public required string WindowTitle
    {
        get
        {
            lock (Lock) return field;
        }
        set
        {
            lock (Lock)
            {
                if (field != value)
                {
                    Interlocked.Exchange(ref field, value);
                    OnWindowTitleChanged?.Invoke(value);
                }
            }
        }
    }

    readonly Lock Lock = new();

    /// <summary>
    /// Updates <see cref="LastKeepAliveTick"/>.
    /// </summary>
    /// <remarks>
    /// Does not cancel a <see cref="Kill"/> if it was issued already.
    /// </remarks>
    /// Note: Even if two threads will try to update the tick counter at the same time
    ///  - the difference in readings will be negligible. So we don't need to use locks here.
    public void KeepAlive() => LastKeepAliveTick = Environment.TickCount64;

    /// <summary>
    /// Kills this <see cref="VTSEndPoint"/>, allowing other services to gradually stop communication with it.
    /// </summary>
    /// <remarks>
    /// This method never throws.
    /// </remarks>
    public void Kill()
    {
        try { CancellationSource.Cancel(); }
        catch (Exception ex) { ex.Out(this); }
    }

    public override string ToString() => $"[{nameof(VTSEndPoint)} #{InstanceID}]";
}
