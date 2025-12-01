using System.Linq;
using Microsoft.Extensions.Logging;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;
using YessGoFront.Models;
#if ANDROID
using Android.Util;
#endif

namespace YessGoFront.Services.Api;

/// <summary>
/// Реализация API сервиса для работы с партнёрами
/// </summary>
public class PartnersApiService : ApiClient, IPartnersApiService
{
    public PartnersApiService(
        HttpClient httpClient,
        ILogger<PartnersApiService>? logger = null)
        : base(httpClient, logger)
    {
    }

    public async Task<IReadOnlyList<PartnerDto>> GetAllAsync(
        CancellationToken ct = default)
    {
        var endpoint = ApiEndpoints.PartnersEndpoints.List;
        
#if ANDROID
        Log.Info("PartnersApiService", $"[GetAllAsync] Запрос партнёров с эндпоинта: {endpoint}");
#endif
        System.Diagnostics.Debug.WriteLine($"[PartnersApiService] Fetching partners from: {endpoint}");
        Logger?.LogInformation("[PartnersApiService] Fetching partners from: {Endpoint}", endpoint);
        
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        
#if ANDROID
        Log.Info("PartnersApiService", $"[GetAllAsync] Получено партнёров: {result?.Count ?? 0}");
#endif
        System.Diagnostics.Debug.WriteLine($"[PartnersApiService] Loaded {result?.Count ?? 0} partners");
        
        if (result != null && result.Count > 0)
        {
            Logger?.LogInformation("[PartnersApiService] Loaded {Count} partners", result.Count);
            // Логируем ВСЕ партнёры для отладки
            foreach (var partner in result)
            {
                var logoUrl = partner.LogoUrl ?? "null";
#if ANDROID
                Log.Info("PartnersApiService", $"[GetAllAsync] Partner: Id={partner.Id}, Name={partner.Name}, LogoUrl={logoUrl}");
#endif
                System.Diagnostics.Debug.WriteLine($"[PartnersApiService] Partner: Id={partner.Id}, Name={partner.Name}, LogoUrl={logoUrl}");
                Logger?.LogInformation("[PartnersApiService] Partner: Id={Id}, Name={Name}, LogoUrl={LogoUrl}", 
                    partner.Id, partner.Name, logoUrl);
            }
        }
        else
        {
#if ANDROID
            Log.Warn("PartnersApiService", "[GetAllAsync] Партнёры не загружены или результат пуст");
#endif
            System.Diagnostics.Debug.WriteLine("[PartnersApiService] No partners loaded or empty result");
            Logger?.LogWarning("[PartnersApiService] No partners loaded or empty result");
        }
        
        return result ?? new List<PartnerDto>();
    }

    public async Task<IReadOnlyList<PartnerDto>> GetByCategoryAsync(
        string category,
        CancellationToken ct = default)
    {
        var endpoint = ApiEndpoints.PartnersEndpoints.ByCategory(category);
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        return result ?? new List<PartnerDto>();
    }

    // Новая реализация: запрос по categoryId через query param
    public async Task<IReadOnlyList<PartnerDto>> GetByCategoryIdAsync(
        int categoryId,
        CancellationToken ct = default)
    {
        var endpoint = $"{ApiEndpoints.PartnersEndpoints.List}?categoryId={categoryId}";
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        return result ?? new List<PartnerDto>();
    }

    public async Task<PartnerDetailDto> GetByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        if (!int.TryParse(id, out var partnerId))
        {
            throw new ArgumentException($"Invalid partner ID: {id}", nameof(id));
        }
        var endpoint = ApiEndpoints.PartnersEndpoints.ById(partnerId);
        return await GetAsync<PartnerDetailDto>(endpoint, ct);
    }

    public async Task<IReadOnlyList<PartnerDto>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        var endpoint = $"{ApiEndpoints.PartnersEndpoints.List}?query={Uri.EscapeDataString(query)}";
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        return result ?? new List<PartnerDto>();
    }

    public async Task<IReadOnlyList<PartnerDto>> GetNearbyAsync(
        double latitude,
        double longitude,
        int radius = 5000,
        CancellationToken ct = default)
    {
        var endpoint = ApiEndpoints.PartnersEndpoints.Nearby(latitude, longitude, radius / 1000.0); // Преобразуем радиус из метров в километры
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        return result ?? new List<PartnerDto>();
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        string partnerId,
        CancellationToken ct = default)
    {
        if (!int.TryParse(partnerId, out var id))
        {
            throw new ArgumentException($"Invalid partner ID: {partnerId}", nameof(partnerId));
        }
        var endpoint = ApiEndpoints.PartnersEndpoints.Products(id);
        
        Logger?.LogInformation("[PartnersApiService] Loading products from endpoint: {Endpoint}", endpoint);
        System.Diagnostics.Debug.WriteLine($"[PartnersApiService] Loading products from: {endpoint}");
        
        try
        {
            // Получаем сырой JSON для логирования и обработки
            var uri = BuildUri(endpoint);
            var response = await HttpClient.GetAsync(uri, ct);
            await EnsureSuccessStatusCode(response);
            
            var jsonContent = await response.Content.ReadAsStringAsync(ct);
            
            // Логируем сырой JSON
            Logger?.LogInformation("[PartnersApiService] Products API raw JSON response (first 2000 chars): {Json}", 
                jsonContent.Length > 2000 ? jsonContent.Substring(0, 2000) + "..." : jsonContent);
            System.Diagnostics.Debug.WriteLine($"[PartnersApiService] Products API raw JSON: {(jsonContent.Length > 2000 ? jsonContent.Substring(0, 2000) + "..." : jsonContent)}");
#if ANDROID
            Log.Info("PartnersApiService", $"[GetProductsAsync] Products API raw JSON (first 2000 chars): {(jsonContent.Length > 2000 ? jsonContent.Substring(0, 2000) + "..." : jsonContent)}");
#endif
            
            // Пытаемся десериализовать как пагинированный ответ (объект с полем "items")
            List<ProductDto>? products = null;
            try
            {
                // Сначала пробуем как пагинированный ответ (формат backend: { "items": [...], "total": 2, ... })
                var pagedResponse = System.Text.Json.JsonSerializer.Deserialize<PagedProductsResponse>(jsonContent, JsonOptions);
                if (pagedResponse != null && pagedResponse.Items != null)
                {
                    products = pagedResponse.Items;
                    Logger?.LogInformation("[PartnersApiService] Successfully deserialized {Count} products from 'items' field (total: {Total}, page: {Page})", 
                        products.Count, pagedResponse.Total, pagedResponse.Page);
                    System.Diagnostics.Debug.WriteLine($"[PartnersApiService] Successfully deserialized {products.Count} products from 'items' field");
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Если не получилось, пробуем как прямой массив
                try
                {
                    products = System.Text.Json.JsonSerializer.Deserialize<List<ProductDto>>(jsonContent, JsonOptions);
                    if (products != null)
                    {
                        Logger?.LogInformation("[PartnersApiService] Successfully deserialized products as direct array");
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Если и это не сработало, пробуем другие форматы
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
                        var root = doc.RootElement;
                        
                        // Проверяем, есть ли поле "data"
                        if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            products = System.Text.Json.JsonSerializer.Deserialize<List<ProductDto>>(dataElement.GetRawText(), JsonOptions);
                            Logger?.LogInformation("[PartnersApiService] Successfully deserialized products from 'data' field");
                        }
                        // Проверяем, есть ли поле "products"
                        else if (root.TryGetProperty("products", out var productsElement) && productsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            products = System.Text.Json.JsonSerializer.Deserialize<List<ProductDto>>(productsElement.GetRawText(), JsonOptions);
                            Logger?.LogInformation("[PartnersApiService] Successfully deserialized products from 'products' field");
                        }
                        else
                        {
                            Logger?.LogWarning("[PartnersApiService] JSON structure is not recognized. Root element type: {ValueKind}", root.ValueKind);
                            System.Diagnostics.Debug.WriteLine($"[PartnersApiService] JSON structure is not recognized. Root element type: {root.ValueKind}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex, "[PartnersApiService] Failed to parse JSON structure. JSON: {Json}", 
                            jsonContent.Length > 1000 ? jsonContent.Substring(0, 1000) + "..." : jsonContent);
                        throw;
                    }
                }
            }
            
            if (products == null)
            {
                Logger?.LogWarning("[PartnersApiService] Products deserialization returned null");
                return new List<ProductDto>();
            }
            
            Logger?.LogInformation("[PartnersApiService] Successfully loaded {Count} products for partner {PartnerId}", products.Count, id);
            System.Diagnostics.Debug.WriteLine($"[PartnersApiService] Successfully loaded {products.Count} products for partner {id}");
            
            return products;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "[PartnersApiService] Error loading products for partner {PartnerId}: {Message}", id, ex.Message);
            System.Diagnostics.Debug.WriteLine($"[PartnersApiService] Error loading products: {ex.Message}");
            throw;
        }
    }
}

