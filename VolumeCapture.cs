using System.Windows.Threading;

namespace VoiceTrigger;

public static class VolumeCapture
{
    public static event Action<bool> OnStateChanged;
    public static event Action<int, float[]> OnVolumeCaptured;
    static readonly DispatcherTimer Timer = new();
    static readonly DispatcherTimer RestartTimer = new();

    static VolumeCapture()
    {
        Timer.Tick += HandleTick;
        RestartTimer.Tick += HandleRestart;
    }

    private static void HandleRestart(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    public static void Start()
    {
        Timer.Tick += HandleTick;

    }

    public static void Stop()
    {

    }

    private static void HandleTick(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }
}
