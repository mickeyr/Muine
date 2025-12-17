using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Muine.App.Converters;

/// <summary>
/// Converts duration in seconds (int) to a formatted time string (MM:SS)
/// </summary>
public class DurationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int durationSeconds)
        {
            var timeSpan = TimeSpan.FromSeconds(durationSeconds);
            
            // Format as HH:MM:SS if duration is >= 1 hour, otherwise MM:SS
            if (timeSpan.TotalHours >= 1)
            {
                return timeSpan.ToString(@"h\:mm\:ss");
            }
            else
            {
                return timeSpan.ToString(@"m\:ss");
            }
        }
        
        return "0:00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
