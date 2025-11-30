using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace YessGoFront.Converters
{
    /// <summary>
    /// Конвертер для проверки равенства строк
    /// </summary>
    public class StringEqualsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string strValue && parameter is string paramValue)
            {
                return strValue == paramValue;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

