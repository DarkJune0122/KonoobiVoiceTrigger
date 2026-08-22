using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceTrigger;

public sealed partial class TriggeredResistance(string display, double resistance) : ObservableObject
{
    public static readonly TriggeredResistance Default = new("Normal", 1);
    [ObservableProperty] public partial string DisplayText { get; set; } = display;
    [ObservableProperty] public partial double Resistance { get; set; } = resistance;
}
