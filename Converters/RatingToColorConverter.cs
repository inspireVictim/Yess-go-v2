using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace YessGoFront.Converters;

/// <summary>
/// Конвертер для изменения цвета кнопки рейтинга в зависимости от выбранного рейтинга
/// </summary>
public class RatingToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentRating && parameter is string paramStr && int.TryParse(paramStr, out int buttonRating))
        {
            // Если рейтинг кнопки меньше или равен текущему рейтингу - зелёный цвет
            if (buttonRating <= currentRating)
            {
                return Color.FromArgb("#0B4A3B");
            }
            // Иначе - серый цвет
            return Color.FromArgb("#E0E0E0");
        }
        
        return Color.FromArgb("#E0E0E0");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

