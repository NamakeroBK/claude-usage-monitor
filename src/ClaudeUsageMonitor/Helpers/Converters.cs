using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ClaudeUsageMonitor.Models;

namespace ClaudeUsageMonitor.Helpers;

/// <summary>
/// Converts UsageLevel enum to appropriate brush color
/// </summary>
public class LevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UsageLevel level)
        {
            return level switch
            {
                UsageLevel.Safe => Application.Current.Resources["SafeBrush"] as SolidColorBrush ?? Brushes.Green,
                UsageLevel.Moderate => Application.Current.Resources["ModerateBrush"] as SolidColorBrush ?? Brushes.Orange,
                UsageLevel.Critical => Application.Current.Resources["CriticalBrush"] as SolidColorBrush ?? Brushes.Red,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts percentage (0-100) to width for progress bar
/// Assumes parent container width of ~180 pixels (adjustable via parameter)
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    private const double DefaultMaxWidth = 180.0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double maxWidth = DefaultMaxWidth;
        
        if (parameter is double paramWidth)
            maxWidth = paramWidth;
        else if (parameter is string strParam && double.TryParse(strParam, out var parsed))
            maxWidth = parsed;

        if (value is int intValue)
            return Math.Max(0, Math.Min(maxWidth, (intValue / 100.0) * maxWidth));
        
        if (value is double doubleValue)
            return Math.Max(0, Math.Min(maxWidth, (doubleValue / 100.0) * maxWidth));

        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to Visibility
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility == Visibility.Visible;
        return false;
    }
}

/// <summary>
/// Inverts boolean to Visibility (true = Collapsed, false = Visible)
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility != Visibility.Visible;
        return true;
    }
}
