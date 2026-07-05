using System.Globalization;
using System.Windows.Data;
using VoiceTrigger.VTS;
using Wpf.Ui.Controls;

namespace VoiceTrigger;


[ValueConversion(typeof(VTSStatus), typeof(bool))]
public sealed class VTSStatusToBoolConverter : AbstractConverter<BoolToVisibilityConverter>
{
    public override object Convert(object value, Type targetType, object invert, CultureInfo culture)
    {
        if (value is not VTSStatus status)
            return invert is not null;

        return status == VTSStatus.Online ? invert is null : invert is not null;
    }

    public override object ConvertBack(object value, Type targetType, object invert, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : AbstractConverter<BoolToVisibilityConverter>
{
    public override object Convert(object value, Type targetType, object invert, CultureInfo culture)
    {
        if (value is not bool flag)
            return invert is null ? Visibility.Visible : Visibility.Collapsed;

        if (invert is not null) flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public override object ConvertBack(object value, Type targetType, object invert, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

[ValueConversion(typeof(double), typeof(GridLength))]
public sealed class DoubleToPercentConverter : AbstractConverter<DoubleToPercentConverter>
{
    public override object Convert(object value, Type targetType, object invert, CultureInfo culture)
    {
        if (value is not double progress)
            return new GridLength(0, GridUnitType.Star);

        progress = Math.Clamp(progress, 0.0, 1.0);
        return new GridLength(progress, GridUnitType.Star);
    }

    public override object ConvertBack(object value, Type targetType, object invert, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

[ValueConversion(typeof(double), typeof(GridLength))]
public sealed class DoubleToRemainingPercentConverter : AbstractConverter<DoubleToRemainingPercentConverter>
{
    public override object Convert(object value, Type targetType, object invert, CultureInfo culture)
    {
        if (value is not double progress)
            return new GridLength(1, GridUnitType.Star);

        progress = Math.Clamp(progress, 0.0, 1.0);
        return new GridLength(1.0 - progress, GridUnitType.Star);
    }

    public override object ConvertBack(object value, Type targetType, object invert, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

[ValueConversion(typeof(bool), typeof(SymbolRegular))]
public sealed class StateToMicrophoneIconConcerter : AbstractConverter<StateToMicrophoneIconConcerter>
{
    public override object Convert(object value, Type targetType, object invert, CultureInfo culture)
    {
        if (value is not bool state) return SymbolRegular.MicOff32;
        if (invert is not null) state = !state;
        return state ? SymbolRegular.Mic32 : SymbolRegular.MicOff32;
    }

    public override object ConvertBack(object value, Type targetType, object invert, CultureInfo culture)
    {
        if (value is not SymbolRegular state) return false;
        return state == SymbolRegular.Mic32 ? invert is null : invert is not null;
    }
}

public abstract class AbstractConverter<T> : IValueConverter where T : AbstractConverter<T>, new()
{
    public static T Instance { get; private set; } = new T();

    public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}