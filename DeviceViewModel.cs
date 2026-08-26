using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;

namespace VoiceTrigger;

public sealed partial class DeviceViewModel : ObservableObject
{
    [ObservableProperty] public partial string ID { get; set; }
    [ObservableProperty] public partial string DisplayName { get; set; }
    [ObservableProperty] public partial MMDevice? Device { get; set; }
    [ObservableProperty] public partial bool IsValid { get; set; }
}
