using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PmGui.Converters
{
    public class BoolToDoubleConverter : IValueConverter
    {
        public double TrueValue { get; set; } = 1.0;
        public double FalseValue { get; set; } = 0.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}