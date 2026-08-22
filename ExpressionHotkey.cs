using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceTrigger;

public sealed partial class ExpressionHotkey : ObservableObject
{
    [ObservableProperty] public partial string? ModelID { get; set; } = string.Empty;
    [ObservableProperty] public partial string? ModelName { get; set; } = "Kobi";
    [ObservableProperty] public partial string? HotkeyID { get; set; } = string.Empty;
    [ObservableProperty] public partial string? HotkeyName { get; set; } = "Alt Colors";
    [ObservableProperty] public partial string? ExpressionFile { get; set; } = string.Empty;
    [ObservableProperty] public partial string? ExpressionFileName { get; set; } = "Expression.exp3.json";
    [ObservableProperty] public partial HotkeyState State { get; set; } = HotkeyState.Disconnected;
}
