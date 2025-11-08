using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace YessGoFront.Converters;

/// <summary>
/// Конвертер для преобразования строки (URL или путь к локальному файлу) в ImageSource
/// Автоматически определяет тип источника и создаёт соответствующий ImageSource
/// </summary>
public class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string imagePath || string.IsNullOrWhiteSpace(imagePath))
        {
            System.Diagnostics.Debug.WriteLine($"[StringToImageSourceConverter] Empty or null image path");
            return null;
        }

        System.Diagnostics.Debug.WriteLine($"[StringToImageSourceConverter] Converting: {imagePath}");

        // Проверяем, является ли это URL
        if (Uri.TryCreate(imagePath, UriKind.Absolute, out var uri) 
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            // Это URL - используем UriImageSource с кэшированием и оптимизацией
            System.Diagnostics.Debug.WriteLine($"[StringToImageSourceConverter] Using UriImageSource for URL: {uri}");
            var imageSource = new UriImageSource
            {
                Uri = uri,
                CachingEnabled = true,
                CacheValidity = TimeSpan.FromDays(7) // Увеличиваем время кэширования
            };
            
            // Предзагрузка изображения для более быстрого отображения
            // MAUI автоматически кэширует изображения при использовании UriImageSource
            
            return imageSource;
        }

        // Это локальный файл - используем FileImageSource
        System.Diagnostics.Debug.WriteLine($"[StringToImageSourceConverter] Using FileImageSource for local file: {imagePath}");
        return ImageSource.FromFile(imagePath);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Обратная конверсия обычно не требуется
        return null;
    }
}

