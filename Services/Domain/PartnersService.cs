using Microsoft.Extensions.Logging;
using YessGoFront.Data;
using YessGoFront.Data.Entities;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Models;
using YessGoFront.Services.Api;
using Microsoft.EntityFrameworkCore;

namespace YessGoFront.Services.Domain;

/// <summary>
/// Реализация Domain сервиса для работы с партнёрами
/// </summary>
public class PartnersService : IPartnersService
{
    private readonly IPartnersApiService _apiService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PartnersService>? _logger;

    public PartnersService(
        IPartnersApiService apiService,
        AppDbContext dbContext,
        ILogger<PartnersService>? logger = null)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<IReadOnlyList<PartnerDto>> GetPartnersByCategoryAsync(
        string category,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be empty", nameof(category));

            _logger?.LogDebug("Getting partners for category: {Category}", category);
            return await _apiService.GetByCategoryAsync(category, ct);
        }
        catch (ApiException)
        {
            // Пробрасываем API исключения дальше
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error getting partners for category {Category}", category);
            throw new NetworkException("Не удалось загрузить партнёров", ex);
        }
    }

    // Новый метод: запрос по id категории
    public async Task<IReadOnlyList<PartnerDto>> GetPartnersByCategoryAsync(
        int categoryId,
        CancellationToken ct = default)
    {
        try
        {
            _logger?.LogDebug("Getting partners for category id: {CategoryId}", categoryId);
            return await _apiService.GetByCategoryIdAsync(categoryId, ct);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error getting partners for category id {CategoryId}", categoryId);
            throw new NetworkException("Не удалось загрузить партнёров", ex);
        }
    }

    public async Task<PartnerDetailDto> GetPartnerByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty", nameof(id));

            _logger?.LogDebug("Getting partner by id: {Id}", id);
            return await _apiService.GetByIdAsync(id, ct);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error getting partner {Id}", id);
            throw new NetworkException("Не удалось загрузить информацию о партнёре", ex);
        }
    }

    public async Task<IReadOnlyList<PartnerDto>> SearchPartnersAsync(
        string query,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<PartnerDto>();

            _logger?.LogDebug("Searching partners with query: {Query}", query);
            return await _apiService.SearchAsync(query, ct);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error searching partners");
            throw new NetworkException("Не удалось выполнить поиск", ex);
        }
    }

    public async Task<IReadOnlyList<PartnerDto>> GetNearbyPartnersAsync(
        double latitude,
        double longitude,
        int radius = 5000,
        CancellationToken ct = default)
    {
        try
        {
            _logger?.LogDebug("Getting nearby partners at {Lat}, {Lon}, radius: {Radius}", 
                latitude, longitude, radius);
            return await _apiService.GetNearbyAsync(latitude, longitude, radius, ct);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error getting nearby partners");
            throw new NetworkException("Не удалось загрузить ближайших партнёров", ex);
        }
    }

    public async Task<IReadOnlyList<ProductDto>> GetPartnerProductsAsync(
        string partnerId,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(partnerId))
                throw new ArgumentException("Partner ID cannot be empty", nameof(partnerId));

            if (!int.TryParse(partnerId, out var partnerIdInt))
            {
                throw new ArgumentException($"Invalid partner ID format: {partnerId}", nameof(partnerId));
            }

            _logger?.LogInformation("🌐 [GetPartnerProductsAsync] Loading products from API for partner: {PartnerId} (parsed to int: {PartnerIdInt})", 
                partnerId, partnerIdInt);
            System.Diagnostics.Debug.WriteLine($"[PartnersService] 🌐 Loading products from API for partner: {partnerId} (parsed to int: {partnerIdInt})");

            // Загружаем товары с API сервера
            var products = await _apiService.GetProductsAsync(partnerId, ct);

            _logger?.LogInformation("✅ [GetPartnerProductsAsync] Successfully loaded {Count} products from API for partner {PartnerId}", 
                products?.Count ?? 0, partnerId);
            System.Diagnostics.Debug.WriteLine($"[PartnersService] ✅ Successfully loaded {products?.Count ?? 0} products from API for partner {partnerId}");

            // Логируем детали каждого продукта для отладки
            if (products != null && products.Any())
            {
                foreach (var p in products)
                {
                    _logger?.LogDebug("  📦 Product: Id={Id}, Name={Name}, Price={Price}, IsAvailable={IsAvailable}", 
                        p.Id, p.Name, p.Price, p.IsAvailable);
                    System.Diagnostics.Debug.WriteLine($"[PartnersService]   📦 Product: Id={p.Id}, Name={p.Name}, Price={p.Price}, IsAvailable={p.IsAvailable}");
                }
            }
            else
            {
                _logger?.LogWarning("⚠️ [GetPartnerProductsAsync] No products found in API response for partnerId {PartnerIdInt}", partnerIdInt);
                System.Diagnostics.Debug.WriteLine($"[PartnersService] ⚠️ No products found in API response for partnerId {partnerIdInt}");
            }

            return products ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ [GetPartnerProductsAsync] Error loading products from API for partner {PartnerId}. " +
                "Error: {ErrorMessage}", partnerId, ex.Message);
            System.Diagnostics.Debug.WriteLine($"[PartnersService] ❌ Error loading products from API: {ex.Message}");
            throw new NetworkException("Не удалось загрузить продукты партнёра с сервера", ex);
        }
    }
}

