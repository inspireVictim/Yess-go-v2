using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Linq;

namespace YessGoFront.Services;

public interface IImageCacheService
{
    Task<SKBitmap?> LoadImageAsync(string? url, CancellationToken cancellationToken = default);
    void ClearCache();
}

/// <summary>
/// Класс для отслеживания времени последнего использования изображения (для LRU)
/// </summary>
internal class CachedImage
{
    public SKBitmap Bitmap { get; set; } = null!;
    public DateTime LastAccessed { get; set; }
    public long SizeBytes { get; set; }
}

public class ImageCacheService : IImageCacheService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageCacheService>? _logger;
    private readonly ConcurrentDictionary<string, CachedImage> _cache = new();
    private readonly SemaphoreSlim _semaphore = new(10); // Максимум 10 одновременных загрузок
    private readonly SemaphoreSlim _cacheLock = new(1, 1); // Для синхронизации операций с кэшем
    
    // Ограничения кэша
    private const int MaxCacheItems = 50; // Максимум 50 изображений
    private const long MaxCacheSizeBytes = 100 * 1024 * 1024; // 100MB
    private const int MaxImageDimension = 1024; // Максимальный размер изображения (для оптимизации памяти)

    public ImageCacheService(IHttpClientFactory httpClientFactory, ILogger<ImageCacheService>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SKBitmap?> LoadImageAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Проверяем кэш и обновляем время последнего использования
        if (_cache.TryGetValue(url, out var cachedImage))
        {
            cachedImage.LastAccessed = DateTime.UtcNow;
            _logger?.LogDebug($"Image loaded from cache: {url}");
            return cachedImage.Bitmap;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Двойная проверка после получения семафора
            if (_cache.TryGetValue(url, out cachedImage))
            {
                cachedImage.LastAccessed = DateTime.UtcNow;
                return cachedImage.Bitmap;
            }

            _logger?.LogInformation($"Loading image from URL: {url}");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning($"Failed to load image: {url}, Status: {response.StatusCode}");
                return null;
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger?.LogWarning($"Empty image data: {url}");
                return null;
            }

            // Создаём SKBitmap из байтов
            var bitmap = SKBitmap.Decode(imageBytes);
            if (bitmap == null)
            {
                _logger?.LogWarning($"Failed to decode image: {url}");
                return null;
            }

            // Оптимизируем размер изображения для экономии памяти
            bitmap = OptimizeImageSize(bitmap);

            // Вычисляем размер в байтах
            var sizeBytes = (long)bitmap.Width * bitmap.Height * 4; // RGBA = 4 байта на пиксель

            // Проверяем и очищаем кэш при необходимости
            await EnsureCacheSpaceAsync(sizeBytes, cancellationToken);

            // Кэшируем с информацией о времени доступа
            var cached = new CachedImage
            {
                Bitmap = bitmap,
                LastAccessed = DateTime.UtcNow,
                SizeBytes = sizeBytes
            };
            
            _cache.TryAdd(url, cached);
            _logger?.LogInformation($"Image loaded and cached: {url}, Size: {bitmap.Width}x{bitmap.Height}, Memory: {sizeBytes / 1024}KB");

            return bitmap;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error loading image: {url}");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Оптимизирует размер изображения для экономии памяти
    /// </summary>
    private SKBitmap OptimizeImageSize(SKBitmap bitmap)
    {
        if (bitmap.Width <= MaxImageDimension && bitmap.Height <= MaxImageDimension)
            return bitmap;

        // Вычисляем масштаб для уменьшения до MaxImageDimension
        var scale = Math.Min((float)MaxImageDimension / bitmap.Width, (float)MaxImageDimension / bitmap.Height);
        var newWidth = (int)(bitmap.Width * scale);
        var newHeight = (int)(bitmap.Height * scale);

        var resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
        if (resized != null && resized != bitmap)
        {
            bitmap.Dispose(); // Освобождаем оригинал
            _logger?.LogDebug($"Image resized from {bitmap.Width}x{bitmap.Height} to {newWidth}x{newHeight}");
            return resized;
        }

        return bitmap;
    }

    /// <summary>
    /// Обеспечивает свободное место в кэше, используя LRU стратегию
    /// </summary>
    private async Task EnsureCacheSpaceAsync(long requiredBytes, CancellationToken cancellationToken)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // Вычисляем текущий размер кэша
            long currentSize = _cache.Values.Sum(c => c.SizeBytes);
            int currentCount = _cache.Count;

            // Если кэш не переполнен, ничего не делаем
            if (currentCount < MaxCacheItems && currentSize + requiredBytes <= MaxCacheSizeBytes)
                return;

            // Сортируем по времени последнего использования (LRU)
            var itemsToRemove = _cache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(Math.Max(1, currentCount - MaxCacheItems + 1))
                .ToList();

            // Удаляем наименее используемые элементы
            foreach (var kvp in itemsToRemove)
            {
                if (_cache.TryRemove(kvp.Key, out var removed))
                {
                    removed.Bitmap?.Dispose();
                    currentSize -= removed.SizeBytes;
                    currentCount--;
                    _logger?.LogDebug($"Removed LRU image from cache: {kvp.Key}");
                }
            }

            // Если все еще не хватает места, удаляем еще элементы
            while (currentSize + requiredBytes > MaxCacheSizeBytes && _cache.Count > 0)
            {
                var oldest = _cache.OrderBy(kvp => kvp.Value.LastAccessed).First();
                if (_cache.TryRemove(oldest.Key, out var removed))
                {
                    removed.Bitmap?.Dispose();
                    currentSize -= removed.SizeBytes;
                    _logger?.LogDebug($"Removed image to free memory: {oldest.Key}");
                }
                else
                {
                    break; // Не удалось удалить, выходим
                }
            }
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task ClearCacheAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            foreach (var cached in _cache.Values)
            {
                cached.Bitmap?.Dispose();
            }
            _cache.Clear();
            _logger?.LogInformation("Image cache cleared");
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    [Obsolete("Use ClearCacheAsync instead to avoid blocking UI thread")]
    public void ClearCache()
    {
        // Для обратной совместимости, но не рекомендуется использовать
        ClearCacheAsync().GetAwaiter().GetResult();
    }
}

