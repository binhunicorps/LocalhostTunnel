using LocalhostTunnel.Desktop.Utilities;
using System.Globalization;
using System.Windows.Data;

namespace LocalhostTunnel.Desktop.Converters;

public sealed class HoChiMinhTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var format = parameter as string ?? "HH:mm:ss";

        if (value is DateTimeOffset dto)
        {
            return AppTimeZone.Format(dto, format);
        }

        if (value is DateTime dt)
        {
            return AppTimeZone.Format(new DateTimeOffset(dt), format);
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
