using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VoiceTrigger.Services;

namespace VoiceTrigger.Audio;

public sealed partial class AudioCaptureService : ObservableObject
{
    public delegate void SelectedAudioDeviceChangedEventHandler(AudioDeviceViewModel model);

    public event SelectedAudioDeviceChangedEventHandler? SelectedAudioDeviceChanged;

    public ObservableCollection<AudioDeviceViewModel> AudioDevices { get; } = [AudioDeviceViewModel.None];
    [ObservableProperty] public partial AudioDeviceViewModel SelectedAudioDevice { get; private set; } = AudioDeviceViewModel.None;

    public AudioCaptureService()
    {
        AudioDevices = [];
    }

    partial void OnSelectedAudioDeviceChanged(AudioDeviceViewModel value)
    {
        SelectedAudioDeviceChanged?.Invoke(value);
        var 
        Roaming.SelectedAudioDevice
    }
}
