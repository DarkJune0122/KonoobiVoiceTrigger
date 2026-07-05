using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using System.Windows.Threading;
using VoiceTrigger.VTS;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger;

// TODO: Serialize states. Move Advanced settings in a separate .cfg file.
// TODO: Go through the logic and find out - matbe we can make the logic funnier to use? More engaging, etc.
public partial class ViewModel : ObservableObject
{
    const long MinBoostSpacingMs = 1000;

    [ObservableProperty] public partial double LeftAudioVolume { get; set; }
    [ObservableProperty] public partial double RightAudioVolume { get; set; }
    [ObservableProperty] public partial bool AudioCaptureEnabled { get; set; }
    [ObservableProperty] public partial DeviceViewModel[] AudioCaptureDevices { get; set; } = [];
    [ObservableProperty] public partial DeviceViewModel? SelectedAudioCaptureDevice { get; set; }
    [ObservableProperty] public partial double ActivationVolume { get; set; } = 0.12;
    [ObservableProperty] public partial double TriggerBreakChargeUpBoost { get; set; } = 0.2;
    [ObservableProperty] public partial double TriggerChargeUp { get; set; } = 3.2;
    [ObservableProperty] public partial double TriggerChargeDown { get; set; } = 16.0;
    [ObservableProperty] public partial bool IsVolumeActivated { get; set; }
    [ObservableProperty] public partial bool Triggered { get; set; }
    [ObservableProperty] public partial double TriggerProgress { get; set; }

    readonly DispatcherTimer CaptureTimer = new();
    MMDevice? ActiveAudioCaptureDevice;
    long LastTick = Environment.TickCount64;
    long LastBoostTick;
    long TotalTicks;

    public ViewModel()
    {
        CaptureTimer.Tick += HandleCaptureTick;
        CaptureTimer.Interval = TimeSpan.FromMicroseconds(100); // 10
        CaptureTimer.IsEnabled = AudioCaptureEnabled;
        RefreshInputDevices();
    }

    [RelayCommand]
    public static async Task ToggleModelColor()
    {
        await VTubeStudio.Instance.Request(new VTSHotkeyTriggerRequest()
        {
            Data = new()
            {
                HotkeyID = "158eb62bdd5d438ca5175516154131dc",
                ItemInstanceID = null,
            },
        });
    }
    [RelayCommand] public void ToggleCapture() => AudioCaptureEnabled = !AudioCaptureEnabled;
    partial void OnAudioCaptureEnabledChanged(bool value)
    {
        CaptureTimer.IsEnabled = value;
        if (!value)
        {
            LastBoostTick = 0;
            LeftAudioVolume = 0;
            RightAudioVolume = 0;
            IsVolumeActivated = false;
            TriggerProgress = 0;
            Triggered = false;
        }
        else
        {
            LastTick = Environment.TickCount64;
        }
    }
    private void HandleCaptureTick(object? sender, EventArgs e)
    {
        Console.WriteLine(nameof(HandleCaptureTick));
        CaptureTickImmediate();
    }

    const long MaxTickCount = 128_000_000_000; // Large enough to never hit, but small enough to never overflow when exceeded.
    [RelayCommand] public void CaptureTick() => Application.Current.Dispatcher.Invoke(CaptureTickImmediate);
    private void CaptureTickImmediate()
    {
        if (ActiveAudioCaptureDevice is null)
        {
            LeftAudioVolume = 0;
            RightAudioVolume = 0;
            Console.WriteLine($"[{DateTime.Now}] Missing input device.");
            return;
        }
        if (ActiveAudioCaptureDevice.State != DeviceState.Active)
        {
            LeftAudioVolume = 0;
            RightAudioVolume = 0;
            Console.WriteLine($"[{DateTime.Now}] Selected input device is not active anymore. Resetting...");
            ActiveAudioCaptureDevice = null;
            return;
        }

        double volume;
        var channels = ActiveAudioCaptureDevice.AudioMeterInformation.PeakValues;
        switch (channels.Count)
        {
            case 0:
                volume = 0f;
                LeftAudioVolume = volume;
                RightAudioVolume = volume;
                Console.WriteLine($"[{DateTime.Now}][0]: {LeftAudioVolume} (s: {TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds})");
                break;
            case 1:
                volume = channels[0];
                LeftAudioVolume = volume;
                RightAudioVolume = volume;
                Console.WriteLine($"[{DateTime.Now}][1]: {LeftAudioVolume} (s: {TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds})");
                break;
            case 2:
                LeftAudioVolume = channels[0];
                RightAudioVolume = channels[1];
                volume = Math.Max(LeftAudioVolume, RightAudioVolume);
                Console.WriteLine($"[{DateTime.Now}][2]: {LeftAudioVolume} + {RightAudioVolume} (s: {TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds})");
                break;
            default:
                if (channels.Count < 0) goto case 0;
                else goto case 2;
        }

        IsVolumeActivated = volume > ActivationVolume;

        long tick = Environment.TickCount64;
        long delta = tick - LastTick;
        if (!Triggered)
        {
            if (IsVolumeActivated)
            {
                TotalTicks = Math.Clamp(TotalTicks + delta, 0, MaxTickCount);
            }
            else
            {
                // Reduces timer to extend the transformation once triggered.
                TotalTicks = Math.Clamp(TotalTicks - delta, 0, MaxTickCount);
            }

            double total = TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds;
            if (total >= TriggerChargeUp)
            {
                Triggered = true;
                TriggerProgress = 1;
                TotalTicks = 0;
            }
            else
            {
                TriggerProgress = total / TriggerChargeUp;
            }
        }
        else
        {
            if (IsVolumeActivated)
            {
                // Reduces timer to extend the transformation once triggered.
                TotalTicks = Math.Clamp(TotalTicks - delta, 0, MaxTickCount);
            }
            else
            {
                TotalTicks = Math.Clamp(TotalTicks + delta, 0, MaxTickCount);
            }

            double total = TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds;
            if (total >= TriggerChargeDown)
            {
                Triggered = false;
                TriggerProgress = 1;
                TotalTicks = 0;
            }
            else
            {
                TriggerProgress = total / TriggerChargeDown;
            }
        }
        LastTick = Environment.TickCount64;
    }

    partial void OnIsVolumeActivatedChanged(bool value)
    {
        long tick = Environment.TickCount64;
        if (!Triggered && value && TimeSpan.FromMilliseconds(tick - LastBoostTick).TotalMilliseconds >= MinBoostSpacingMs)
        {
            LastBoostTick = tick;
            TotalTicks = Math.Clamp(TotalTicks + (long)TimeSpan.FromSeconds(TriggerBreakChargeUpBoost).TotalMilliseconds, 0, MaxTickCount);
        }
    }

    partial void OnTriggeredChanged(bool value)
    {
        // TODO: Send state to Live2D.
    }

    [RelayCommand] public void RefreshInputDevices() => Application.Current.Dispatcher.Invoke(RefreshInputDevicesImmediate);
    private void RefreshInputDevicesImmediate()
    {
        Console.WriteLine("Restart");
        var collection = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        if (collection.Count == 0)
        {
            SelectedAudioCaptureDevice = null;
            ActiveAudioCaptureDevice = null;
            AudioCaptureDevices = [];
            return;
        }

        string id;
        AudioCaptureDevices = [.. collection.Select(static d => new DeviceViewModel() { ID = d.ID, DisplayName = d.FriendlyName })];
        if (SelectedAudioCaptureDevice is null)
        {
            ActiveAudioCaptureDevice = collection[0];
        }
        else
        {
            id = SelectedAudioCaptureDevice.ID;
            ActiveAudioCaptureDevice = collection.FirstOrDefault(d => d.ID == id) ?? collection[0];
        }

        id = ActiveAudioCaptureDevice.ID;
        SelectedAudioCaptureDevice = AudioCaptureDevices.FirstOrDefault(d => d.ID == id);
    }
}
