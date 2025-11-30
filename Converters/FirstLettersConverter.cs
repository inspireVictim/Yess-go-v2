using System;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;

namespace YessGoFront.Converters;

/// <summary>
/// Конвертер для получения первых букв из строки (для логотипов)
/// </summary>
public class FirstLettersConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Разбиваем на слова и берём первые буквы каждого слова
        var words = text.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        // Берём первые буквы (максимум 2-3 буквы)
        var firstLetters = string.Join("", words.Take(2).Select(w => w.Length > 0 ? w[0].ToString().ToUpper() : ""));
        
        // Если получилось больше 3 символов, обрезаем
        if (firstLetters.Length > 3)
        {
            firstLetters = firstLetters.Substring(0, 3);
        }

        return firstLetters;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

