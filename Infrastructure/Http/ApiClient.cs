using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Exceptions;

namespace YessGoFront.Infrastructure.Http;

/// <summary>
/// Базовый класс для всех API клиентов
/// </summary>
public abstract class ApiClient
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger? Logger;
    protected readonly JsonSerializerOptions JsonOptions;

    protected ApiClient(HttpClient httpClient, ILogger? logger = null)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger;

        // ✅ Принудительно используем HTTP/1.1 (фикс "ResponseEnded" на Android/.NET 8/9)
        HttpClient.DefaultRequestVersion = new Version(1, 1);
        HttpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        // ✅ Гарантируем корректный BaseAddress
        // BaseAddress должен быть установлен в MauiProgram.cs через HttpClient конфигурацию
        // Если не установлен - используем дефолтный (для обратной совместимости)
        if (HttpClient.BaseAddress == null)
        {
#if ANDROID
            // ✅ Для эмулятора Android: 10.0.2.2 (специальный alias для хоста)
            // ✅ Для реального телефона: IP компьютера в сети
            // 📌 Установите переменную окружения перед запуском:
            //    API_BASE_URL=http://YOUR_HOST_IP:8000/
            // 📌 Или отредактируйте значение по умолчанию ниже:
            
            // Используем ту же логику определения эмулятора, что и в MauiProgram.cs
            var fingerprint = Android.OS.Build.Fingerprint ?? "";
            var model = Android.OS.Build.Model ?? "";
            var product = Android.OS.Build.Product ?? "";
            var manufacturer = Android.OS.Build.Manufacturer ?? "";
            
            var isEmulator = 
                fingerprint.Contains("generic", StringComparison.OrdinalIgnoreCase) || 
                fingerprint.Contains("emulator", StringComparison.OrdinalIgnoreCase) ||
                fingerprint.Contains("sdk", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("Emulator", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("emulator", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("sdk", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("gphone", StringComparison.OrdinalIgnoreCase) ||
                product.Contains("emulator", StringComparison.OrdinalIgnoreCase) ||
                product.Contains("sdk", StringComparison.OrdinalIgnoreCase) ||
                product.Contains("gphone", StringComparison.OrdinalIgnoreCase) ||
                manufacturer.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
                manufacturer.Equals("Genymotion", StringComparison.OrdinalIgnoreCase);
            
            var apiUrl = Environment.GetEnvironmentVariable("API_BASE_URL") 
                ?? (isEmulator ? "http://10.0.2.2:8000/" : "http://192.168.0.67:8000/");
            
            HttpClient.BaseAddress = new Uri(apiUrl);
            Logger?.LogWarning("[ApiClient] Android: BaseAddress установлен на: {Url} (Emulator: {IsEmulator})", 
                apiUrl, isEmulator);
#else
            // 📌 Для WinUI/Desktop: используйте localhost
            HttpClient.BaseAddress = new Uri("http://localhost:8000/");
            Logger?.LogWarning("[ApiClient] Desktop: BaseAddress установлен на localhost");
#endif
        }
        
        // Логируем используемый URL для отладки
        Logger?.LogInformation("[ApiClient] Using BaseAddress: {BaseAddress}", HttpClient.BaseAddress);
        
        if (!HttpClient.BaseAddress.ToString().EndsWith("/"))
        {
            HttpClient.BaseAddress = new Uri(HttpClient.BaseAddress + "/");
        }

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected Uri BuildUri(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint is empty", nameof(endpoint));

        endpoint = endpoint.TrimStart('/');
        return new Uri(HttpClient.BaseAddress!, endpoint);
    }

    protected async Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("GET {Url}", uri);

            var response = await HttpClient.GetAsync(uri, ct);
            await EnsureSuccessStatusCode(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct)
                   ?? throw new ApiException("Не удалось десериализовать ответ сервера");
        }
        catch (Exception ex) when (NetworkException.IsNetworkError(ex))
        {
            throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
        }
    }

    protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken ct = default)
    {
        try
        {
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("POST {Url}", uri);

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PostAsync(uri, content, ct);
            await EnsureSuccessStatusCode(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct)
                   ?? throw new ApiException("Не удалось десериализовать ответ сервера");
        }
        catch (Exception ex) when (NetworkException.IsNetworkError(ex))
        {
            throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
        }
    }

    protected async Task PostAsync<TRequest>(string endpoint, TRequest request, CancellationToken ct = default)
    {
        try
        {
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("POST {Url}", uri);

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PostAsync(uri, content, ct);
            await EnsureSuccessStatusCode(response);
        }
        catch (Exception ex) when (NetworkException.IsNetworkError(ex))
        {
            throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
        }
    }

    protected async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken ct = default)
    {
        try
        {
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("PUT {Url}", uri);

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PutAsync(uri, content, ct);
            await EnsureSuccessStatusCode(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct)
                   ?? throw new ApiException("Не удалось десериализовать ответ сервера");
        }
        catch (Exception ex) when (NetworkException.IsNetworkError(ex))
        {
            throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
        }
    }

    protected async Task DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("DELETE {Url}", uri);

            var response = await HttpClient.DeleteAsync(uri, ct);
            await EnsureSuccessStatusCode(response);
        }
        catch (Exception ex) when (NetworkException.IsNetworkError(ex))
        {
            throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
        }
    }

    protected async Task EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorContent = await response.Content.ReadAsStringAsync();
        Logger?.LogError("API Error: {StatusCode} - {Content}", response.StatusCode, errorContent);

        // Извлекаем сообщение из JSON ответа
        var errorMessage = ExtractErrorMessage(errorContent);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new UnauthorizedException(errorMessage ?? "Требуется авторизация"),
            HttpStatusCode.Forbidden => new ForbiddenException(errorMessage ?? "Доступ запрещён"),
            HttpStatusCode.NotFound => new NotFoundException(errorMessage ?? "Ресурс не найден"),
            HttpStatusCode.BadRequest => new BadRequestException(errorMessage ?? "Неверный запрос", errorContent),
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable
                => new ServerException(errorMessage ?? "Ошибка сервера", response.StatusCode),
            _ => new ApiException(errorMessage ?? $"API error: {response.StatusCode}", response.StatusCode)
        };
    }

    private static string? ExtractErrorMessage(string? errorContent)
    {
        if (string.IsNullOrWhiteSpace(errorContent))
            return null;

        try
        {
            // Пытаемся распарсить JSON и извлечь поле "message"
            using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
            if (doc.RootElement.TryGetProperty("message", out var messageElement))
            {
                var message = messageElement.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            
            // Если нет "message", пробуем "detail"
            if (doc.RootElement.TryGetProperty("detail", out var detailElement))
            {
                var detail = detailElement.GetString();
                if (!string.IsNullOrWhiteSpace(detail))
                    return detail;
            }
        }
        catch
        {
            // Если не удалось распарсить JSON, возвращаем null
        }

        return null;
    }
}
