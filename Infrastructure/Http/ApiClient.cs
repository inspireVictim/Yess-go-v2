using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using YessGoFront.Config;
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

        // ✅ BaseAddress должен быть установлен в MauiProgram.cs через HttpClient конфигурацию
        // Все HTTP-запросы используют централизованную конфигурацию из ApiConfiguration
        // Если BaseAddress не установлен (что не должно происходить) - используем централизованную конфигурацию
        if (HttpClient.BaseAddress == null)
        {
            var baseUrl = ApiConfiguration.GetBaseUrlWithTrailingSlash();
            HttpClient.BaseAddress = new Uri(baseUrl);
            Logger?.LogWarning("[ApiClient] BaseAddress был null, установлен из ApiConfiguration: {BaseAddress}", baseUrl);
        }
        
        // Логируем используемый URL для отладки
        Logger?.LogInformation("[ApiClient] Using BaseAddress: {BaseAddress}", HttpClient.BaseAddress);
        
        // Гарантируем наличие завершающего слеша
        if (!HttpClient.BaseAddress.ToString().EndsWith("/"))
        {
            HttpClient.BaseAddress = new Uri(HttpClient.BaseAddress + "/");
        }

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Не используем PropertyNamingPolicy, так как бэкенд использует snake_case через JsonPropertyName атрибуты
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

            try
            {
                return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct)
                       ?? throw new ApiException("Не удалось десериализовать ответ сервера");
            }
            catch (InvalidCastException ex)
            {
                Logger?.LogError(ex, "InvalidCastException при десериализации {Type} из {Url}", typeof(TResponse).Name, uri);
                var content = await response.Content.ReadAsStringAsync(ct);
                Logger?.LogError("Response content (first 500 chars): {Content}", content.Substring(0, Math.Min(500, content.Length)));
                throw new ApiException($"Ошибка приведения типов при десериализации: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                Logger?.LogError(ex, "JsonException при десериализации {Type} из {Url}", typeof(TResponse).Name, uri);
                var content = await response.Content.ReadAsStringAsync(ct);
                Logger?.LogError("Response content (first 500 chars): {Content}", content.Substring(0, Math.Min(500, content.Length)));
                throw new ApiException($"Ошибка формата JSON: {ex.Message}", ex);
            }
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

    protected async Task<TResponse> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken ct = default)
    {
        try
        {
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("PATCH {Url}", uri);

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PatchAsync(uri, content, ct);
            await EnsureSuccessStatusCode(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct)
                   ?? throw new ApiException("Не удалось десериализовать ответ сервера");
        }
        catch (Exception ex) when (NetworkException.IsNetworkError(ex))
        {
            throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
        }
    }

    protected async Task PatchAsync<TRequest>(string endpoint, TRequest request, CancellationToken ct = default)
    {
        try
        {
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("PATCH {Url}", uri);

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PatchAsync(uri, content, ct);
            await EnsureSuccessStatusCode(response);
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
            // Пытаемся распарсить JSON и извлечь поле "message", "error" или "detail"
            using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
            
            // Проверяем "message"
            if (doc.RootElement.TryGetProperty("message", out var messageElement))
            {
                var message = messageElement.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            
            // Проверяем "error" (некоторые API возвращают ошибки в этом поле)
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                var error = errorElement.GetString();
                if (!string.IsNullOrWhiteSpace(error))
                    return error;
            }
            
            // Проверяем "detail"
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
