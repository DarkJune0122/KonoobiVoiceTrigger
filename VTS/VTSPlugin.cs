using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceTrigger.VTS;

public sealed partial class VTSPlugin(ITokenStorage storage, string? name = null, string? developer = null, string? icon = null) : ObservableObject
{
    public const string? DefaultName = "Unknown";
    public const string? DefaultDeveloper = "Unknown";
    public const string? DefaultIcon = null;

    [ObservableProperty] public partial string? Name { get; private set; } = name ?? DefaultName;
    [ObservableProperty] public partial string? Developer { get; private set; } = developer ?? DefaultDeveloper;
    [ObservableProperty] public partial string? Icon { get; private set; } = icon ?? DefaultIcon;
    [ObservableProperty] public partial ITokenStorage TokenStorage { get; set; } = storage;

    readonly Lock Lock = new();
    public void Set(string? name, string? developer = null, string? icon = null)
    {
        name ??= DefaultName;
        developer ??= DefaultDeveloper;
        icon ??= DefaultIcon;

        lock (Lock)
        {
            Name = name;
            Developer = developer;
            Icon = icon;
        }
    }
}
