using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceTrigger;

public sealed partial class ModelHotkey : ObservableObject
{
    [ObservableProperty] public partial string? ModelID { get; set; } = string.Empty;
    [ObservableProperty] public partial string? ModelName { get; set; } = "Kobi";
    [ObservableProperty] public partial string? HotkeyID { get; set; } = string.Empty;
    [ObservableProperty] public partial string? HotkeyName { get; set; } = "Alt Colors";
    [ObservableProperty] public partial string? ExpressionFile { get; set; } = "Expression.exp3.json";
    [ObservableProperty] public partial HotkeyLinkState LinkState { get; set; }
}
