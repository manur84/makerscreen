using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MakerScreen.Management.Converters;

/// <summary>
/// Helper class to enable binding to PasswordBox.Password property.
/// This is a workaround for WPF's intentional non-support of Password binding for security reasons.
/// 
/// SECURITY CONSIDERATION: This helper stores the password as a plain string in memory,
/// which may be visible in memory dumps or during debugging. For production applications
/// handling sensitive credentials, consider:
/// - Using SecureString with proper disposal
/// - Storing credentials in Windows Credential Manager
/// - Using Azure Key Vault or similar secure storage
/// - Implementing proper credential encryption
/// 
/// This implementation is suitable for internal configuration tools where the security
/// risk is acceptable for the use case (e.g., database connection strings for overlays).
/// </summary>
public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false, OnBindPasswordChanged));

    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached(
            "UpdatingPassword",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false));

    public static void SetBindPassword(DependencyObject dp, bool value)
    {
        dp.SetValue(BindPasswordProperty, value);
    }

    public static bool GetBindPassword(DependencyObject dp)
    {
        return (bool)dp.GetValue(BindPasswordProperty);
    }

    public static string GetBoundPassword(DependencyObject dp)
    {
        return (string)dp.GetValue(BoundPasswordProperty);
    }

    public static void SetBoundPassword(DependencyObject dp, string value)
    {
        dp.SetValue(BoundPasswordProperty, value);
    }

    private static bool GetUpdatingPassword(DependencyObject dp)
    {
        return (bool)dp.GetValue(UpdatingPasswordProperty);
    }

    private static void SetUpdatingPassword(DependencyObject dp, bool value)
    {
        dp.SetValue(UpdatingPasswordProperty, value);
    }

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordBox passwordBox)
        {
            passwordBox.PasswordChanged -= HandlePasswordChanged;

            if (!GetUpdatingPassword(passwordBox))
            {
                passwordBox.Password = (string)e.NewValue ?? string.Empty;
            }

            passwordBox.PasswordChanged += HandlePasswordChanged;
        }
    }

    private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is PasswordBox passwordBox)
        {
            if ((bool)e.OldValue)
            {
                passwordBox.PasswordChanged -= HandlePasswordChanged;
            }

            if ((bool)e.NewValue)
            {
                passwordBox.PasswordChanged += HandlePasswordChanged;
            }
        }
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            SetUpdatingPassword(passwordBox, true);
            SetBoundPassword(passwordBox, passwordBox.Password);
            SetUpdatingPassword(passwordBox, false);
        }
    }
}

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
        // Also handle null as "zero" (show message)
        if (value == null)
        {
            return Visibility.Visible;
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
/// Converts null to Visibility (null = Visible, not null = Collapsed)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts not-null to Visibility (null = Collapsed, not null = Visible)
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
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

/// <summary>
/// Converts OverlayType enum to Visibility based on parameter (shows panel when type matches)
/// </summary>
public class OverlayTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MakerScreen.Core.Models.OverlayType overlayType && parameter is string typeString)
        {
            // Parse the expected type from parameter
            if (Enum.TryParse<MakerScreen.Core.Models.OverlayType>(typeString, out var expectedType))
            {
                return overlayType == expectedType ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
