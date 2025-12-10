using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Config;
#if ANDROID
using Android.Util;
#endif

namespace YessGoFront.Infrastructure.Http;

/// <summary>
/// Базовый класс для всех API клиентов
/// </summary>
public abstract class ApiClient
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger? Logger;
    protected readonly JsonSerializerOptions JsonOptions;

    // Таймаут по умолчанию для HTTP запросов (30 секунд)
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    protected ApiClient(HttpClient httpClient, ILogger? logger = null)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger;

        // ✅ Принудительно используем HTTP/1.1 (фикс "ResponseEnded" на Android/.NET 8/9)
        HttpClient.DefaultRequestVersion = new Version(1, 1);
        HttpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        
        // ✅ Устанавливаем таймаут по умолчанию для всех запросов
        if (HttpClient.Timeout == TimeSpan.FromSeconds(100)) // Значение по умолчанию
        {
            HttpClient.Timeout = DefaultTimeout;
        }

        // ✅ BaseAddress должен быть установлен в MauiProgram.cs через HttpClient конфигурацию
        // Проверяем, что BaseAddress установлен
        if (HttpClient.BaseAddress == null)
        {
            // Если BaseAddress не установлен, используем значение из ApiConfiguration
            var defaultUrl = ApiConfiguration.GetBaseUrlWithTrailingSlash();
            HttpClient.BaseAddress = new Uri(defaultUrl);
            Logger?.LogWarning("[ApiClient] BaseAddress не был установлен через DI, используется значение по умолчанию: {Url}", defaultUrl);
        }
        
        // Логируем используемый URL для отладки
        Logger?.LogInformation("[ApiClient] Using BaseAddress: {BaseAddress}", HttpClient.BaseAddress);
        
        // Убеждаемся, что BaseAddress заканчивается на "/"
        if (!HttpClient.BaseAddress.ToString().EndsWith("/"))
        {
            HttpClient.BaseAddress = new Uri(HttpClient.BaseAddress + "/");
        }
        
#if ANDROID
        Android.Util.Log.Info("ApiClient", $"BaseAddress установлен: {HttpClient.BaseAddress}");
        System.Diagnostics.Debug.WriteLine($"[ApiClient] BaseAddress: {HttpClient.BaseAddress}");
#endif

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // НЕ используем PropertyNamingPolicy.CamelCase, так как модели используют [JsonPropertyName] для точного указания имён полей
            // PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            // Проверяем, что BaseAddress установлен
            if (HttpClient.BaseAddress == null)
            {
                var errorMsg = "HttpClient.BaseAddress is null. Cannot make GET request.";
                Logger?.LogError("[ApiClient] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("GET {Url}", uri);
            Logger?.LogInformation("[ApiClient] Attempting GET to: {Url} (BaseAddress: {BaseAddress}, Endpoint: {Endpoint})", 
                uri, HttpClient.BaseAddress, endpoint);

            // Создаем CancellationToken с таймаутом, если не передан
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!ct.CanBeCanceled)
            {
                cts.CancelAfter(DefaultTimeout);
            }

            var response = await HttpClient.GetAsync(uri, cts.Token);
            await EnsureSuccessStatusCode(response);

            // Читаем JSON как строку для логирования и десериализации
            var jsonContent = await response.Content.ReadAsStringAsync(cts.Token);
            
            // Для отладки: логируем сырой JSON ответ (для партнёров и баннеров)
            if (endpoint.Contains("partners", StringComparison.OrdinalIgnoreCase) || endpoint.Contains("banners", StringComparison.OrdinalIgnoreCase))
            {
                var entityType = endpoint.Contains("partners", StringComparison.OrdinalIgnoreCase) ? "Partners" : "Banners";
                
                // Проверяем формат JSON (camelCase vs snake_case) для logoUrl
                var hasLogoUrl = jsonContent.Contains("\"logoUrl\"", StringComparison.OrdinalIgnoreCase);
                var hasLogoUrlSnake = jsonContent.Contains("\"logo_url\"", StringComparison.OrdinalIgnoreCase);
                
#if ANDROID
                Android.Util.Log.Info("ApiClient", $"[GetAsync] {entityType} JSON format check: logoUrl={hasLogoUrl}, logo_url={hasLogoUrlSnake}");
                var preview = jsonContent.Length > 5000 ? jsonContent.Substring(0, 5000) + "..." : jsonContent;
                Android.Util.Log.Info("ApiClient", $"[GetAsync] {entityType} API JSON response (first 5000 chars):\n{preview}");
                // Также логируем полный JSON, если он не слишком большой
                if (jsonContent.Length <= 10000)
                {
                    Android.Util.Log.Info("ApiClient", $"[GetAsync] {entityType} API FULL JSON response:\n{jsonContent}");
                }
#endif
                System.Diagnostics.Debug.WriteLine($"[ApiClient] {entityType} JSON format: logoUrl={hasLogoUrl}, logo_url={hasLogoUrlSnake}");
                System.Diagnostics.Debug.WriteLine($"[ApiClient] {entityType} API response (first 5000 chars): {(jsonContent.Length > 5000 ? jsonContent.Substring(0, 5000) + "..." : jsonContent)}");
                Logger?.LogInformation("[ApiClient] {EntityType} JSON format: logoUrl={HasLogoUrl}, logo_url={HasLogoUrlSnake}", 
                    entityType, hasLogoUrl, hasLogoUrlSnake);
                Logger?.LogInformation("[ApiClient] {EntityType} API response (first 1000 chars): {Json}", 
                    entityType, jsonContent.Length > 1000 ? jsonContent.Substring(0, 1000) + "..." : jsonContent);
            }

            // Десериализуем из строки
            var result = System.Text.Json.JsonSerializer.Deserialize<TResponse>(jsonContent, JsonOptions);
            if (result == null)
            {
                throw new ApiException("Не удалось десериализовать ответ сервера");
            }
            
            return result;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            Logger?.LogError(ex, "[ApiClient] Request timeout for GET {Endpoint}", endpoint);
            throw new NetworkException("Превышено время ожидания ответа от сервера. Проверьте подключение к интернету.", ex);
        }
        catch (Exception ex) when (NetworkException.IsNetworkError(ex))
        {
            Logger?.LogError(ex, "[ApiClient] Network error for GET {Endpoint}: {Message}", endpoint, ex.Message);
            throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
        }
    }

    protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken ct = default)
    {
        try
        {
            // Проверяем, что BaseAddress установлен
            if (HttpClient.BaseAddress == null)
            {
                var errorMsg = "HttpClient.BaseAddress is null. Cannot make POST request.";
                Logger?.LogError("[ApiClient] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("POST {Url}", uri);
            Logger?.LogInformation("[ApiClient] Attempting POST to: {Url} (BaseAddress: {BaseAddress}, Endpoint: {Endpoint})", 
                uri, HttpClient.BaseAddress, endpoint);

            // Создаем CancellationToken с таймаутом, если не передан
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!ct.CanBeCanceled)
            {
                cts.CancelAfter(DefaultTimeout);
            }

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PostAsync(uri, content, cts.Token);
            await EnsureSuccessStatusCode(response);

            var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cts.Token);
            if (result == null)
            {
                Logger?.LogError("[ApiClient] Failed to deserialize response from POST {Endpoint}", endpoint);
                throw new ApiException("Не удалось десериализовать ответ сервера");
            }
            
            return result;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            var uri = BuildUri(endpoint);
            Logger?.LogError(ex, "[ApiClient] Request timeout for POST {Url}", uri);
            throw new NetworkException("Превышено время ожидания ответа от сервера. Проверьте подключение к интернету.", ex);
        }
        catch (Exception ex) when (NetworkException.IsNetworkError(ex))
        {
            var uri = BuildUri(endpoint);
            Logger?.LogError(ex, "[ApiClient] Network error during POST to {Url}: {Message}", uri, ex.Message);
            
            // Детальная информация об ошибке
            var errorDetails = ex switch
            {
                HttpRequestException httpEx => $"HTTP Error: {httpEx.Message}",
                System.Net.Sockets.SocketException sockEx => $"Socket Error: {sockEx.Message} (ErrorCode: {sockEx.ErrorCode})",
                TaskCanceledException => "Request timeout - сервер не отвечает",
                _ => ex.Message
            };
            
            throw new NetworkException($"Ошибка сети при подключении к {uri.Host}: {errorDetails}", ex);
        }
    }

    protected async Task PostAsync<TRequest>(string endpoint, TRequest request, CancellationToken ct = default)
    {
        try
        {
            var uri = BuildUri(endpoint);
            Logger?.LogDebug("POST {Url}", uri);

            // Создаем CancellationToken с таймаутом, если не передан
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!ct.CanBeCanceled)
            {
                cts.CancelAfter(DefaultTimeout);
            }

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PostAsync(uri, content, cts.Token);
            await EnsureSuccessStatusCode(response);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            var uri = BuildUri(endpoint);
            Logger?.LogError(ex, "[ApiClient] Request timeout for POST {Url}", uri);
            throw new NetworkException("Превышено время ожидания ответа от сервера. Проверьте подключение к интернету.", ex);
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

            // Создаем CancellationToken с таймаутом, если не передан
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!ct.CanBeCanceled)
            {
                cts.CancelAfter(DefaultTimeout);
            }

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PutAsync(uri, content, cts.Token);
            await EnsureSuccessStatusCode(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cts.Token)
                   ?? throw new ApiException("Не удалось десериализовать ответ сервера");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            var uri = BuildUri(endpoint);
            Logger?.LogError(ex, "[ApiClient] Request timeout for PUT {Url}", uri);
            throw new NetworkException("Превышено время ожидания ответа от сервера. Проверьте подключение к интернету.", ex);
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

            // Создаем CancellationToken с таймаутом, если не передан
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!ct.CanBeCanceled)
            {
                cts.CancelAfter(DefaultTimeout);
            }

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PatchAsync(uri, content, cts.Token);
            await EnsureSuccessStatusCode(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cts.Token)
                   ?? throw new ApiException("Не удалось десериализовать ответ сервера");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            var uri = BuildUri(endpoint);
            Logger?.LogError(ex, "[ApiClient] Request timeout for PATCH {Url}", uri);
            throw new NetworkException("Превышено время ожидания ответа от сервера. Проверьте подключение к интернету.", ex);
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

            // Создаем CancellationToken с таймаутом, если не передан
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!ct.CanBeCanceled)
            {
                cts.CancelAfter(DefaultTimeout);
            }

            var content = JsonContent.Create(request, options: JsonOptions);
            var response = await HttpClient.PatchAsync(uri, content, cts.Token);
            await EnsureSuccessStatusCode(response);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            var uri = BuildUri(endpoint);
            Logger?.LogError(ex, "[ApiClient] Request timeout for PATCH {Url}", uri);
            throw new NetworkException("Превышено время ожидания ответа от сервера. Проверьте подключение к интернету.", ex);
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

            // Создаем CancellationToken с таймаутом, если не передан
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!ct.CanBeCanceled)
            {
                cts.CancelAfter(DefaultTimeout);
            }

            var response = await HttpClient.DeleteAsync(uri, cts.Token);
            await EnsureSuccessStatusCode(response);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            var uri = BuildUri(endpoint);
            Logger?.LogError(ex, "[ApiClient] Request timeout for DELETE {Url}", uri);
            throw new NetworkException("Превышено время ожидания ответа от сервера. Проверьте подключение к интернету.", ex);
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
        var requestUri = response.RequestMessage?.RequestUri?.ToString() ?? "unknown";
        var requestMethod = response.RequestMessage?.Method?.ToString() ?? "unknown";
        
        Logger?.LogError("[ApiClient] API Error: {Method} {StatusCode} - Request: {RequestUri} - Response: {Content}", 
            requestMethod, response.StatusCode, requestUri, errorContent);
        
        // Для 404 ошибок логируем дополнительную информацию
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Logger?.LogError("[ApiClient] 404 Not Found Details: " +
                "BaseAddress={BaseAddress}, " +
                "RequestUri={RequestUri}, " +
                "ResponseHeaders={Headers}, " +
                "ResponseBody={Body}",
                HttpClient.BaseAddress,
                requestUri,
                string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")),
                errorContent);
        }

        // Извлекаем сообщение из JSON ответа
        var errorMessage = ExtractErrorMessage(errorContent);

        // Для 404 ошибок добавляем более информативное сообщение
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var detailedMessage = errorMessage ?? "Ресурс не найден";
            
            // Проверяем, является ли это auth endpoint
            if (requestUri.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
            {
                detailedMessage = $"Endpoint не найден на сервере: {requestUri}. " +
                                 $"Возможно, сервер использует другой путь или endpoint не реализован. " +
                                 $"Оригинальное сообщение: {detailedMessage}";
                Logger?.LogError("[ApiClient] Auth endpoint not found: {RequestUri}. Error content: {ErrorContent}", 
                    requestUri, errorContent);
            }
            else
            {
                detailedMessage = $"Ресурс не найден: {requestUri}. {detailedMessage}";
            }
            
            throw new NotFoundException(detailedMessage);
        }

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new UnauthorizedException(errorMessage ?? "Требуется авторизация"),
            HttpStatusCode.Forbidden => new ForbiddenException(errorMessage ?? "Доступ запрещён"),
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
