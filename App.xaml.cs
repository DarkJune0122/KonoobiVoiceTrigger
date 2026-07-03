using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
    static WasapiCapture? Capture;
    static WaveInEvent? WaveIn1;
    static WaveInEvent? WaveIn2;
    static MMDevice? Device;

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

        Device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
        Console.WriteLine(Device.FriendlyName);
        Console.WriteLine(string.Join(", ", Device.Properties));

        Capture = new WasapiCapture(Device, false, 20);
        Capture.DataAvailable += HandleRecording;
        Capture.StartRecording();
        Capture.RecordingStopped += Capture_RecordingStopped;
        Console.WriteLine(Capture.WaveFormat);
        Console.WriteLine(Capture.ShareMode);
        /*WaveIn1 = new WaveInEvent
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(48000, 1)
        };
        WaveIn1.DataAvailable += (s, e) =>
        {
            Console.WriteLine("[0] " + e.BytesRecorded);
        };
        WaveIn1.StartRecording();

        WaveIn2 = new WaveInEvent
        {
            DeviceNumber = 1,
            WaveFormat = new WaveFormat(48000, 1)
        };
        WaveIn2.DataAvailable += (s, e) =>
        {
            Console.WriteLine("[1] " + e.BytesRecorded);
        };
        WaveIn2.DataAvailable += (s, e) =>
        {
            short peak = 0;

            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                short abs = (short)Math.Abs(sample);

                if (abs > peak)
                    peak = abs;
            }

            Console.WriteLine(peak);
        };
        WaveIn2.StartRecording();

        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            var caps = WaveIn.GetCapabilities(i);
            Console.WriteLine($"{i}: {caps.ProductName}");
        }*/
    }

    private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Stopped: {e.Exception}");
        Console.ForegroundColor = ConsoleColor.White;
    }

    private void HandleRecording(object? sender, WaveInEventArgs e)
    {
        WasapiCapture? capture = Capture;
        if (capture is null) return;

        Console.WriteLine($"Bytes: {e.BytesRecorded}");
        try
        {
            switch (capture.WaveFormat.BitsPerSample)
            {
                case 8: Handle8bit(e); return;
                case 16: Handle16bit(e); return;
                case 32: Handle32bit(e); return;
                default: Console.WriteLine($"Unsupported bit sample rate: {capture.WaveFormat.BitsPerSample}"); return; // TODO: Log on screen,
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType()}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void Handle8bit(WaveInEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void Handle16bit(WaveInEventArgs e)
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

    private void Handle32bit(WaveInEventArgs e)
    {
        var samples = MemoryMarshal.Cast<byte, float>(e.Buffer.AsSpan(0, e.BytesRecorded));

        float peak = 0;
        foreach (float sample in samples)
        {
            float abs = Math.Abs(sample);
            if (abs > peak)
                peak = abs;
        }

        Dispatcher.BeginInvoke(() => OnCapturedVolume?.Invoke(peak));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}