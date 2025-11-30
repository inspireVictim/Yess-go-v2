using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace YessGoFront.Converters;

/// <summary>
/// Конвертер для проверки, что значение null
/// Возвращает true, если значение null, false - если не null
/// </summary>
public class IsNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
        
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

