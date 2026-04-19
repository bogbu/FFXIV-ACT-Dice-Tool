using System.Globalization;
using System.Windows.Data;

namespace FFXIVActDiceTool.Helpers;

public class EnumToBooleanConverter : IValueConverter
{
    public static EnumToBooleanConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? parameter : System.Windows.Data.Binding.DoNothing;
    }
}
