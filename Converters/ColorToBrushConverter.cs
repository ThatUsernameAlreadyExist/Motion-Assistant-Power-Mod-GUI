// Converters/ColorToBrushConverter.cs
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Windows11Settings.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is Color color)
                return new SolidColorBrush(color);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}