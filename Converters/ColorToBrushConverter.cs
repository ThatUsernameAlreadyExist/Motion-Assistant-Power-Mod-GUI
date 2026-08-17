// Converters/ColorToBrushConverter.cs
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PmGui.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        // Cache brushes by color to avoid allocating a new SolidColorBrush
        // on every binding.
        private static readonly Dictionary<Color, SolidColorBrush> _brushCache
            = new Dictionary<Color, SolidColorBrush>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is Color color)
            {
                SolidColorBrush brush;
                if (!_brushCache.TryGetValue(color, out brush))
                {
                    brush = new SolidColorBrush(color);
                    _brushCache[color] = brush;
                }
                return brush;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
