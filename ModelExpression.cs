using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceTrigger;

public sealed partial class ModelExpression : ObservableObject
{
    [ObservableProperty] public partial string? ExpressionFile { get; set; } = "Expression.exp3.json";
    [ObservableProperty] public partial HotkeyLinkState LinkState { get; set; }
}
