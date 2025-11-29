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

            _logger?.LogInformation("🔍 [GetPartnerProductsAsync] Starting query for partner: {PartnerId} (parsed to int: {PartnerIdInt})", 
                partnerId, partnerIdInt);
            System.Diagnostics.Debug.WriteLine($"[PartnersService] 🔍 Starting query for partner: {partnerId} (parsed to int: {partnerIdInt})");

            // Проверяем общее количество продуктов в БД для отладки
            int totalProductsCount = 0;
            try
            {
                totalProductsCount = await _dbContext.PartnerProducts.CountAsync(ct);
                _logger?.LogInformation("📊 [GetPartnerProductsAsync] Total products in database: {TotalCount}", totalProductsCount);
                System.Diagnostics.Debug.WriteLine($"[PartnersService] 📊 Total products in database: {totalProductsCount}");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "⚠️ Error counting products, trying raw SQL");
                System.Diagnostics.Debug.WriteLine($"[PartnersService] ⚠️ Error counting products: {ex.Message}");
                
                // Пробуем использовать raw SQL как fallback
                try
                {
                    var connection = _dbContext.Database.GetDbConnection();
                    await connection.OpenAsync(ct);
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM partner_products";
                    var rawCount = await command.ExecuteScalarAsync(ct);
                    totalProductsCount = Convert.ToInt32(rawCount ?? 0);
                    _logger?.LogInformation("📊 [GetPartnerProductsAsync] Raw SQL count: {Count}", totalProductsCount);
                    System.Diagnostics.Debug.WriteLine($"[PartnersService] 📊 Raw SQL count: {totalProductsCount}");
                }
                catch (Exception sqlEx)
                {
                    _logger?.LogError(sqlEx, "❌ Failed to count products even with raw SQL");
                    System.Diagnostics.Debug.WriteLine($"[PartnersService] ❌ Failed to count products even with raw SQL: {sqlEx.Message}");
                }
            }

            // Проверяем количество продуктов для этого партнёра (включая недоступные)
            var partnerProductsCount = await _dbContext.PartnerProducts
                .Where(p => p.PartnerId == partnerIdInt)
                .CountAsync(ct);
            _logger?.LogInformation("📊 [GetPartnerProductsAsync] Total products for partner {PartnerIdInt}: {Count} (including unavailable)", 
                partnerIdInt, partnerProductsCount);
            System.Diagnostics.Debug.WriteLine($"[PartnersService] 📊 Total products for partner {partnerIdInt}: {partnerProductsCount} (including unavailable)");

            // Получаем продукты из базы данных, отсортированные по ID
            List<PartnerProduct> products = new List<PartnerProduct>();
            try
            {
                products = await _dbContext.PartnerProducts
                    .Where(p => p.PartnerId == partnerIdInt && p.IsAvailable)
                    .OrderBy(p => p.Id)
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "⚠️ Error with LINQ query, trying raw SQL. Error: {ErrorMessage}", ex.Message);
                System.Diagnostics.Debug.WriteLine($"[PartnersService] ⚠️ Error with LINQ query: {ex.Message}");
                
                // Fallback: используем FromSqlRaw для прямого SQL запроса
                try
                {
                    _logger?.LogInformation("🔄 [GetPartnerProductsAsync] Using FromSqlRaw fallback for partner {PartnerIdInt}", partnerIdInt);
                    System.Diagnostics.Debug.WriteLine($"[PartnersService] 🔄 Using FromSqlRaw fallback for partner {partnerIdInt}");
                    
                    // Используем FromSqlRaw с параметрами EF Core ({0}, {1}, ...)
                    var rawProducts = await _dbContext.PartnerProducts
                        .FromSqlRaw(
                            "SELECT * FROM partner_products WHERE partner_id = {0} AND (is_available = 1 OR is_available = 'true') ORDER BY id",
                            partnerIdInt)
                        .ToListAsync(ct);
                    
                    products = rawProducts;
                    _logger?.LogInformation("✅ [GetPartnerProductsAsync] Loaded {Count} products using FromSqlRaw", products.Count);
                    System.Diagnostics.Debug.WriteLine($"[PartnersService] ✅ Loaded {products.Count} products using FromSqlRaw");
                }
                catch (Exception fromSqlEx)
                {
                    _logger?.LogError(fromSqlEx, "❌ Failed to load products with FromSqlRaw, trying direct connection. Error: {ErrorMessage}", fromSqlEx.Message);
                    System.Diagnostics.Debug.WriteLine($"[PartnersService] ❌ Failed with FromSqlRaw: {fromSqlEx.Message}");
                    
                    // Последняя попытка - прямой доступ к соединению с правильным синтаксисом SQLite
                    try
                    {
                        var connection = _dbContext.Database.GetDbConnection();
                        var wasClosed = connection.State != System.Data.ConnectionState.Open;
                        if (wasClosed)
                        {
                            await connection.OpenAsync(ct);
                        }
                        
                        using var command = connection.CreateCommand();
                        // SQLite использует позиционные параметры ? без имени
                        command.CommandText = @"
                            SELECT id, partner_id, name, description, ingredients, image_url, weight, 
                                   price, original_price, discount_percent, yess_coins, is_available, category,
                                   created_at, updated_at
                            FROM partner_products 
                            WHERE partner_id = ? AND (is_available = 1 OR is_available = 'true' OR is_available = 'True')
                            ORDER BY id";
                        
                        // В SQLite позиционные параметры добавляются по порядку
                        var param = command.CreateParameter();
                        param.Value = partnerIdInt;
                        command.Parameters.Add(param);
                        
                        products = new List<PartnerProduct>();
                        using var reader = await command.ExecuteReaderAsync(ct);
                        while (await reader.ReadAsync(ct))
                        {
                            products.Add(new PartnerProduct
                            {
                                Id = reader.GetInt32(0),
                                PartnerId = reader.GetInt32(1),
                                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Ingredients = reader.IsDBNull(4) ? null : reader.GetString(4),
                                ImageUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Weight = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Price = reader.GetDecimal(7),
                                OriginalPrice = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                                DiscountPercent = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                                YessCoins = reader.IsDBNull(10) ? null : reader.GetDecimal(10),
                                IsAvailable = reader.GetBoolean(11),
                                Category = reader.IsDBNull(12) ? null : reader.GetString(12),
                                CreatedAt = reader.IsDBNull(13) ? DateTime.UtcNow : reader.GetDateTime(13),
                                UpdatedAt = reader.IsDBNull(14) ? null : reader.GetDateTime(14)
                            });
                        }
                        
                        _logger?.LogInformation("✅ [GetPartnerProductsAsync] Loaded {Count} products using raw SQL", products.Count);
                        System.Diagnostics.Debug.WriteLine($"[PartnersService] ✅ Loaded {products.Count} products using raw SQL");
                    }
                    catch (Exception rawSqlEx)
                    {
                        _logger?.LogError(rawSqlEx, "❌ Failed to load products even with raw SQL");
                        System.Diagnostics.Debug.WriteLine($"[PartnersService] ❌ Failed to load products even with raw SQL: {rawSqlEx.Message}");
                        // Оставляем products как пустой список, не бросаем исключение
                        products = new List<PartnerProduct>();
                    }
                }
            }

            _logger?.LogInformation("✅ [GetPartnerProductsAsync] Found {Count} AVAILABLE products in database for partnerId {PartnerIdInt}", 
                products.Count, partnerIdInt);
            System.Diagnostics.Debug.WriteLine($"[PartnersService] ✅ Found {products.Count} AVAILABLE products in database for partnerId {partnerIdInt}");

            // Логируем детали каждого продукта для отладки
            if (products.Any())
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
                _logger?.LogWarning("⚠️ [GetPartnerProductsAsync] No available products found for partnerId {PartnerIdInt}. " +
                    "Total products for this partner: {TotalCount}", partnerIdInt, partnerProductsCount);
                System.Diagnostics.Debug.WriteLine($"[PartnersService] ⚠️ No available products found for partnerId {partnerIdInt}. Total products for this partner: {partnerProductsCount}");
            }

            // Преобразуем Entity в DTO, маппинг всех полей из базы данных
            var productDtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                PartnerId = p.PartnerId,
                Name = p.Name ?? string.Empty,
                Description = p.Description,
                Ingredients = p.Ingredients,
                ImageUrl = p.ImageUrl,
                Weight = p.Weight,
                Price = p.Price,
                OriginalPrice = p.OriginalPrice,
                DiscountPercent = p.DiscountPercent,
                YessCoins = p.YessCoins,
                IsAvailable = p.IsAvailable,
                Category = p.Category
            }).ToList();

            _logger?.LogInformation("✅ [GetPartnerProductsAsync] Successfully loaded {Count} products from database for partner {PartnerId}", 
                productDtos.Count, partnerId);
            return productDtos;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ [GetPartnerProductsAsync] Unexpected error getting products from database for partner {PartnerId}. " +
                "Error: {ErrorMessage}", partnerId, ex.Message);
            throw new NetworkException("Не удалось загрузить продукты партнёра", ex);
        }
    }
}

