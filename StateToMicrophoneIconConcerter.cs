using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace VoiceTrigger;

[ValueConversion(typeof(bool), typeof(SymbolRegular))]
public sealed class StateToMicrophoneIconConcerter : AbstractConverter<StateToMicrophoneIconConcerter>
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool state) return SymbolRegular.MicOff32;
        return state ? SymbolRegular.Mic32 : SymbolRegular.MicOff32;
    }

    public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SymbolRegular state) return false;
        return state == SymbolRegular.Mic32;
    }
}

public abstract class AbstractConverter<T> : IValueConverter where T : AbstractConverter<T>, new()
{
    public static T Instance { get; private set; } = new T();

    public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}