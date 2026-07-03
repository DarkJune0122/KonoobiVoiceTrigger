using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using System.Windows;
using System.Windows.Threading;

namespace VoiceTrigger;

// TODO: Serialize states.
public partial class ViewModel : ObservableObject
{
    [ObservableProperty] public partial float LeftAudioVolume { get; set; }
    [ObservableProperty] public partial float RightAudioVolume { get; set; }
    [ObservableProperty] public partial bool AudioCaptureEnabled { get; set; }
    [ObservableProperty] public partial DeviceViewModel[] AudioCaptureDevices { get; set; } = [];
    [ObservableProperty] public partial DeviceViewModel? SelectedAudioCaptureDevice { get; set; }

    readonly DispatcherTimer CaptureTimer = new();
    readonly DispatcherTimer RestartTimer = new();
    MMDevice? ActiveAudioCaptureDevice;

    public ViewModel()
    {
        CaptureTimer.Tick += HandleCaptureTick;
        CaptureTimer.Interval = TimeSpan.FromMicroseconds(100);
        CaptureTimer.Start();

        RestartTimer.Tick += HandleRestartDevice;
        RestartTimer.Interval = TimeSpan.FromMicroseconds(100);
        RefreshDevices();
    }

    private void HandleCaptureTick(object? sender, EventArgs e)
    {
        RestartTimer.Stop();
        CaptureTick();
    }
    [RelayCommand] public void CaptureTick() => Application.Current.Dispatcher.Invoke(CaptureTickImmediate);
    private void CaptureTickImmediate()
    {
        if (ActiveAudioCaptureDevice is null)
        {
            Console.WriteLine("Missing input device.");
            return;
        }
        if (ActiveAudioCaptureDevice.State != DeviceState.Active)
        {
            Console.WriteLine("Selected input device is not active anymore. Resetting...");
            ActiveAudioCaptureDevice = null;
            return;
        }
        var channels = ActiveAudioCaptureDevice.AudioMeterInformation.PeakValues;
        switch (channels.Count)
        {
            case 0:
                LeftAudioVolume = RightAudioVolume = 0f;
                break;
            case 1:
                LeftAudioVolume = RightAudioVolume = channels[0];
                break;
            case 2:
                LeftAudioVolume = channels[0];
                RightAudioVolume = channels[1];
                break;
            default:
                if (channels.Count < 0) goto case 0;
                else goto case 2;
        }
    }

    [RelayCommand] public void ToggleCapture() => AudioCaptureEnabled = !AudioCaptureEnabled;
    partial void OnAudioCaptureEnabledChanged(bool value) => CaptureTimer.IsEnabled = value;
    partial void OnSelectedAudioCaptureDeviceChanged(DeviceViewModel? value)
    {
        RestartTimer.Stop();
        RestartTimer.Start();
    }

    [RelayCommand] public void RefreshDevices() => Application.Current.Dispatcher.Invoke(RefreshDevicesImmediate);
    private void RefreshDevicesImmediate()
    {
        var collection = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        DeviceViewModel[] devices = [.. collection.Select(static d => new DeviceViewModel() { ID = d.ID, DisplayName = d.FriendlyName })];
        AudioCaptureDevices = devices;
    }

    private void HandleRestartDevice(object? sender, EventArgs e) => RestartDevice();
    [RelayCommand] public void RestartDevice() => RestartDeviceDispatched();
    private void RestartDeviceDispatched()
    {
        if (SelectedAudioCaptureDevice is null)
        {
            ActiveAudioCaptureDevice = null;
            return;
        }

        if (ActiveAudioCaptureDevice is null || ActiveAudioCaptureDevice.State != DeviceState.Active)
        {
            string id = SelectedAudioCaptureDevice.ID;
            if (string.IsNullOrEmpty(id)) return;
            var devices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            var device = devices.FirstOrDefault((d) => d.ID == id);
            if (device is null) return;
            ActiveAudioCaptureDevice = device;
        }
    }
}
