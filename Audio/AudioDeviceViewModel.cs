using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using System.Diagnostics.CodeAnalysis;

namespace VoiceTrigger.Audio;

public sealed partial class AudioDeviceViewModel : ObservableObject
{
    public static readonly AudioDeviceViewModel None = new();

    [MemberNotNullWhen(true, nameof(ID))]
    [MemberNotNullWhen(true, nameof(FriendlyName))]
    [MemberNotNullWhen(true, nameof(Device))]
    [ObservableProperty] public partial bool IsValid { get; private set; }
    [ObservableProperty] public partial string? ID { get; private set; }
    [ObservableProperty] public partial string? FriendlyName { get; private set; }
    [ObservableProperty] public partial MMDevice? Device { get; private set; }

    public void SetDevice(MMDevice? device)
    {
        if (Device == device)
        {
            return;
        }
        if (device is null)
        {
            IsValid = false; // Updated first.
            Device = null;
            ID = null;
            FriendlyName = null;
        }
        else
        {
            Device = device;
            ID = device.ID;
            FriendlyName = device.FriendlyName;
            IsValid = true; // Updated last.
        }
    }
}
