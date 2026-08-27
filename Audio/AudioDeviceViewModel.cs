using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;

namespace VoiceTrigger.Audio;

public sealed partial class AudioDeviceViewModel : ObservableObject
{
    public static readonly AudioDeviceViewModel None = new()
    {
        ID = string.Empty,
        FriendlyName = "(None)",
        Device = null,
        IsActive = false,
    };

    [ObservableProperty] public partial string ID { get; set; }
    [ObservableProperty] public partial string FriendlyName { get; set; }
    [ObservableProperty] public partial MMDevice? Device { get; set; }
    [MemberNotNullWhen(true, nameof(Device))]
    [ObservableProperty] public partial bool IsActive { get; private set; }
    [ObservableProperty] public partial bool ShowInactiveWarning { get; private set; }

    partial void OnDeviceChanged(MMDevice? value) => UpdateState();
    public void UpdateState()
    {
        IsActive = Device is { State: DeviceState.Active };
        ShowInactiveWarning = !IsActive && this != None;
    }

    public override string ToString() => $"{FriendlyName} ({ID})";
}
