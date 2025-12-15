using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace YessGoFront.Converters;

/// <summary>
/// Инвертирует результат IntToBoolConverter (true если <= 0)
/// </summary>
public class InverseIntToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return intValue <= 0;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

