using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceTrigger;

public sealed partial class SamplingRate(int samples) : ObservableObject
{
    public static readonly SamplingRate Default = new(30);
    [ObservableProperty] public partial string DisplayText { get; set; } = $"{samples}/s";
    [ObservableProperty] public partial int SamplesPerSecond { get; set; } = samples;
}
