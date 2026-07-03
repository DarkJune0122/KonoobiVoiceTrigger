using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceTrigger;

public sealed partial class DeviceViewModel : ObservableObject
{
    [ObservableProperty] public partial string ID { get; set; }
    [ObservableProperty] public partial string DisplayName { get; set; }
    public override int GetHashCode() => ID.GetHashCode();
}
