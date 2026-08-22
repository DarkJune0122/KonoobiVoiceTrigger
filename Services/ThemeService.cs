using Wpf.Ui.Appearance;

namespace VoiceTrigger.Services;

public sealed class ThemeService : Wpf.Ui.ThemeService
{
    public static ThemeService Instance { get; } = new();
    public void NextTheme()
    {
        var currentTheme = GetTheme();
        SetTheme(currentTheme switch
        {
            ApplicationTheme.Light => ApplicationTheme.Dark,
            //ApplicationTheme.Dark => ApplicationTheme.HighContrast,
            _ => ApplicationTheme.Light
        });
    }
}
