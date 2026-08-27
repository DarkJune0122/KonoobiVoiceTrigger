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

    public override bool SetTheme(ApplicationTheme applicationTheme)
    {
        if (base.SetTheme(applicationTheme))
        {
            //SetAccent(applicationTheme == ApplicationTheme.Dark
            //    ? Color.FromRgb(234, 234, 3)
            //    : Color.FromRgb(250, 250, 4));
            UpdateCustomThemeDictionary(applicationTheme);
            return true;
        }
        return false;
    }

    private static void UpdateCustomThemeDictionary(ApplicationTheme theme)
    {
        if (Application.Current is null)
            return;

        foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
        {
            if (dictionary is Styles.ThemesDictionary customDictionary)
                customDictionary.Theme = theme;
        }
    }
}
