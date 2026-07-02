using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace VoiceTrigger;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static event Action<float>? OnCapturedVolume;
    static readonly DispatcherTimer Timer = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Process process = Process.GetProcessById(Environment.ProcessId);
        var all = Process.GetProcessesByName(process.ProcessName);
        if (all.Length > 1)
        {
            Shutdown();
            return;
        }

        var capture = new WasapiCapture();
        capture.DataAvailable += HandleRecording;
        capture.StartRecording();

        //var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

        //Timer.Interval = TimeSpan.FromMilliseconds(20);
        //Timer.Tick += (_, _) =>
        //{
        //    foreach (var d in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        //    {
        //        Console.WriteLine($"[{d.FriendlyName}]: {d.AudioMeterInformation.MasterPeakValue} {d.AudioMeterInformation.HardwareSupport}");
        //    }
        //    //OnCapturedVolume?.Invoke(device.AudioMeterInformation);
        //};
        //Timer.Start();
    }

    private void HandleRecording(object? sender, NAudio.Wave.WaveInEventArgs e)
    {
        short max = 0;

        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = BitConverter.ToInt16(e.Buffer, i);
            short abs = Math.Abs(sample);

            if (abs > max)
                max = abs;
        }

        float level = max / (float)short.MaxValue;
        Dispatcher.BeginInvoke(() => OnCapturedVolume?.Invoke(level));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}