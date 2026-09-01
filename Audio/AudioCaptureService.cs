using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System.Collections.ObjectModel;
using VoiceTrigger.Collections;
using VoiceTrigger.Collections.Pooling;
using VoiceTrigger.Logging;

namespace VoiceTrigger.Audio;

public sealed partial class AudioCaptureService : ObservableObject, IMMNotificationClient
{
    public delegate void SelectedAudioDeviceChangedEventHandler(AudioDeviceViewModel? model);

    static readonly MMDeviceEnumerator DeviceEnumerator = new();
    public static readonly AudioCaptureService Instance = new();

    public event SelectedAudioDeviceChangedEventHandler? SelectedAudioDeviceChanged;

    public ObservableCollection<AudioDeviceViewModel> AudioDevices { get; }
    [ObservableProperty] public partial AudioDeviceViewModel? SelectedAudioDevice { get; set; }

    public AudioCaptureService()
    {
        // .ctor should be initialized from App.OnStartup() UI callback.
        var desc = Local.SelectedAudioDevice;
        if (desc is null || string.IsNullOrWhiteSpace(desc.ID))
        {
            AudioDevices = [AudioDeviceViewModel.None];
            SelectedAudioDevice = AudioDeviceViewModel.None;
        }
        else
        {
            var construct = new AudioDeviceViewModel()
            {
                ID = desc.ID,
                FriendlyName = desc.FriendlyName,
                Device = null,
            };
            AudioDevices = [AudioDeviceViewModel.None, construct];
            SelectedAudioDevice = construct; // Updated last intentionally.
        }
    }

    public void Initialize()
    {
        DeviceEnumerator.RegisterEndpointNotificationCallback(this);
        RebuildDevicesImmediate();
        //WaveIn wave = new();
        //wave.
        //wave.DeviceNumber;
        //wave.StartRecording();

        //Was
    }
    public void Terminate()
    {
        DeviceEnumerator.UnregisterEndpointNotificationCallback(this);
    }

    partial void OnSelectedAudioDeviceChanged(AudioDeviceViewModel? value)
    {
        try
        {
            $"{this} Selected audio device changing to: {value}".Out(ConsoleColor.Gray);
            value?.UpdateState();
            SerializeSelectedAudioDevice();
            SelectedAudioDeviceChanged?.Invoke(value);
            $"{this} Selected audio device changed.".Out(ConsoleColor.Gray);
        }
        catch (Exception ex) { ex.Out($"{this} Failed to process the change of selected audio device!\n"); }
    }

    void SerializeSelectedAudioDevice()
    {
        if (SelectedAudioDevice is null || SelectedAudioDevice == AudioDeviceViewModel.None)
        {
            var option = Local.SelectedAudioDevice;
            Local.SelectedAudioDevice = null;
            if (option is not null)
            {
                option.ID = null!;
                option.FriendlyName = null!;
                Pool.Return(option);
            }
        }
        else
        {
            var options = Local.SelectedAudioDevice;
            if (options is null)
            {
                options = Pool.Rent<AudioDeviceDescriptor>();
                Local.SelectedAudioDevice = options;
            }
            options.ID = SelectedAudioDevice.ID;
            options.FriendlyName = SelectedAudioDevice.FriendlyName;
        }
    }

    readonly HashSet<AudioDeviceViewModel> UnmatchedDevices = [];
    [RelayCommand] public void RebuildDevices() => Application.Current.Dispatcher.BeginInvoke(RebuildDevicesImmediate);
    private void RebuildDevicesImmediate()
    {
        $"{this} Refreshing all input devices...".Out(ConsoleColor.Gray);
        MMDevice? device = null;
        try
        {
            var collection = DeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            // Any devices from the unmatched collection will be disposed.
            UnmatchedDevices.Clear();
            foreach (var temp in AudioDevices)
                UnmatchedDevices.Add(temp);

            // Selected audio device should never be removed.
            if (SelectedAudioDevice is not null)
                UnmatchedDevices.Remove(SelectedAudioDevice);
            UnmatchedDevices.Remove(AudioDeviceViewModel.None);

            if (collection.Count == 0)
            {
                $"{this} Found zero audio devices! Will default to just a selected one.".Out(ConsoleColor.Yellow);
            }
            foreach (var t in collection)
            {
                device = t; // Assigns for disposal in case of an exception.
                string id = device.ID;
                var match = AudioDevices.FirstOrDefault(d => d.ID == id);
                if (match is null)
                {
                    // Found new device.
                    var model = Pool.Rent<AudioDeviceViewModel>();
                    model.ID = device.ID;
                    model.FriendlyName = device.FriendlyName;
                    model.Device = device;
                    device = null; // Prevents disposal.
                    AudioDevices.Add(model);
                    $"{this} Found new audio capture device: {model}".Out(ConsoleColor.Gray);
                }
                else
                {
                    // Found an existing device.
                    UnmatchedDevices.Remove(match);
                    if (match.Device is null)
                    {
                        match.Device = device;
                    }
                    else device.Dispose();
                    device = null;
                }
            }

            // Devices that were removed since the last fetch.
            foreach (var unmatched in UnmatchedDevices)
            {
                AudioDevices.Remove(unmatched);
                unmatched.Device?.Dispose();
                unmatched.Device = null;
                Pool.Return(unmatched);
            }
        }
        catch (Exception ex) { ex.Out($"{this} Failed to update audio devices list!\n"); }
        finally
        {
            try { device?.Dispose(); } catch (Exception ex) { ex.Out($"{this} Failed to dispose retrieved device!\n"); }
            UnmatchedDevices.Clear();
        }
    }


    #region Device Notification Client Implementation
    public void OnDeviceAdded(string pwstrDeviceId) => Application.Current.Dispatcher.BeginInvoke(() => OnDeviceAddedImmediate(pwstrDeviceId));
    void OnDeviceAddedImmediate(string pwstrDeviceId)
    {
        MMDevice? device = null;
        try
        {
            device = DeviceEnumerator.GetDevice(pwstrDeviceId);
            if (device is null) return;
            if (device.DataFlow != DataFlow.Capture) return;

            $"{this} Received new device notification. ID: {pwstrDeviceId}".Out(ConsoleColor.Gray);
            if (SelectedAudioDevice is not null && device.ID == SelectedAudioDevice.ID)
            {
                $"{this} Notified about the selected device. Device state will be updated.".Out(ConsoleColor.Gray);
                SelectedAudioDevice.FriendlyName = device.FriendlyName;
                SelectedAudioDevice.Device?.Dispose();
                SelectedAudioDevice.Device = device;
                device = null;
                SerializeSelectedAudioDevice();
            }
            else
            {
                var match = AudioDevices.FirstOrDefault(d => d.ID == pwstrDeviceId);
                if (match is null)
                {
                    var model = Pool.Rent<AudioDeviceViewModel>();
                    model.ID = device.ID;
                    model.FriendlyName = device.FriendlyName;
                    model.Device = device;
                    AudioDevices.Add(model);
                    device = null;
                }
                else
                {
                    $"{this} Notified about an existing device. Skipping...".Out(ConsoleColor.Gray);
                }
            }
        }
        catch (Exception ex) { ex.Out($"{this} Failed to add new device!\n"); }
        finally
        {
            try { device?.Dispose(); } catch (Exception ex) { ex.Out($"{this} Failed to dispose retrieved device!\n"); }
        }
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => Application.Current.Dispatcher.BeginInvoke(() => OnDeviceStateChangedImmediate(deviceId, newState));
    void OnDeviceStateChangedImmediate(string deviceId, DeviceState newState)
    {
        MMDevice? device = null;
        try
        {
            device = DeviceEnumerator.GetDevice(deviceId);
            if (device is null) return;
            if (device.DataFlow != DataFlow.Capture) return;

            $"{this} Device state change detected. State: {newState}, ID: {deviceId}".Out(ConsoleColor.Gray);
            var match = AudioDevices.FirstOrDefault(d => d.ID == deviceId);
            if (match is null)
            {
                if (newState == DeviceState.Active)
                {
                    var model = Pool.Rent<AudioDeviceViewModel>();
                    model.ID = device.ID;
                    model.FriendlyName = device.FriendlyName;
                    model.Device = device;
                    AudioDevices.Add(model);
                    device = null;
                    $"{this} Added previously missing device.".Out(ConsoleColor.Gray);
                }
                else
                {
                    $"{this} Device is unknown and inactive. Skipping...".Out(ConsoleColor.Gray);
                }
            }
            else
            {
                if (newState == DeviceState.Active && (match.Device is null || match.Device.State != DeviceState.Active))
                {
                    // Renews reference in case state was missing a device.
                    match.FriendlyName = device.FriendlyName;
                    match.Device?.Dispose();
                    match.Device = device;
                    device = null;
                }
                else device.Dispose();

                match.UpdateState();
                $"{this} Device state updated.".Out(ConsoleColor.Gray);
            }
        }
        catch (Exception ex) { ex.Out($"{this} Failed to update device state!\n"); }
        finally
        {
            try { device?.Dispose(); } catch (Exception ex) { ex.Out($"{this} Failed to dispose retrieved device!\n"); }
        }
    }

    public void OnDeviceRemoved(string deviceId) => Application.Current.Dispatcher.BeginInvoke(() => OnDeviceRemovedImmediate(deviceId));
    void OnDeviceRemovedImmediate(string deviceId)
    {
        try
        {
            int index = AudioDevices.FindIndex(d => d.ID == deviceId);
            if (index == -1) return;
            var match = AudioDevices[index];

            $"{this} Received device removal notification for device: {match}".Out(ConsoleColor.Gray);
            if (match == SelectedAudioDevice)
            {
                match.Device?.Dispose();
                match.Device = null;
                $"{this} Notified about the selected device. Device state updated.".Out(ConsoleColor.Gray);
            }
            else
            {
                $"{this} Delisting removed device...".Out(ConsoleColor.Gray);
                AudioDevices.RemoveAt(index);
                match.Device?.Dispose();
                match.Device = null;
                Pool.Return(match);
                $"{this} Device delisted successfully.".Out(ConsoleColor.Gray);
            }
        }
        catch (Exception ex) { ex.Out($"{this} Failed to add new device!\n"); }
    }

    // Note: Maybe add device switching as an option in a LocalConfiguration file.
    void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) { /* Ignored */ }
    void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { /* Ignored */ }

    #endregion

    public override string ToString() => $"[{nameof(AudioCaptureService)}]";
}