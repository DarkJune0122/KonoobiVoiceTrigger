using System.Windows.Markup;
using Wpf.Ui.Appearance;

namespace VoiceTrigger.Styles;

[Localizability(LocalizationCategory.Ignore)]
[Ambient]
[UsableDuringInitialization(true)]
public class ThemesDictionary : ResourceDictionary
{
    /// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===<![CDATA[
    /// .
    /// .                                              Public Properties
    /// .
    /// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===]]>
    public ApplicationTheme Theme
    {
        set
        {
            var themeName = value switch
            {
                ApplicationTheme.Dark => "Dark",
                ApplicationTheme.HighContrast => "HighContrast",
                _ => "Light"
            };

            Source = new Uri($"pack://application:,,,/VoiceTrigger;component/Styles/{themeName}.xaml", UriKind.Absolute);
        }
    }
}
