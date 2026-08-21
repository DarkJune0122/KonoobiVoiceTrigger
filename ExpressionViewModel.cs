using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceTrigger;

public sealed partial class ExpressionViewModel : ObservableObject
{
    [ObservableProperty] public partial string ModelID { get; set; }
    [ObservableProperty] public partial string ModelName { get; set; }
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string DisplayName { get; set; }
    [ObservableProperty] public partial bool Exists { get; set; }
}