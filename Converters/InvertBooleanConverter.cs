using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Windows11Settings.Converters
{
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is bool boolValue)
            {
                return !boolValue;
            }
            return true; // Default behavior: return true for non-boolean values
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is bool boolValue)
            {
                return !boolValue;
            }
            return false; // Default behavior: return false for non-boolean values
        }
    }
}