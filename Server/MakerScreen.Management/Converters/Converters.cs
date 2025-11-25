using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MakerScreen.Management.Converters;

/// <summary>
/// Converts zero count to Visible, non-zero to Collapsed
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return value;
    }
}

/// <summary>
/// Converts a boolean to Visibility
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Converts ClientStatus enum to appropriate color brush
/// </summary>
public class ClientStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MakerScreen.Core.Models.ClientStatus status)
        {
            return status switch
            {
                MakerScreen.Core.Models.ClientStatus.Online => Application.Current.Resources["SuccessBrush"],
                MakerScreen.Core.Models.ClientStatus.Offline => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 153, 153)),
                MakerScreen.Core.Models.ClientStatus.Error => Application.Current.Resources["ErrorBrush"],
                MakerScreen.Core.Models.ClientStatus.Installing => Application.Current.Resources["AccentBrush"],
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 153, 153))
            };
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 153, 153));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
