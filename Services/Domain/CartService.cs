using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using YessGoFront.Models;

namespace YessGoFront.Services.Domain;

/// <summary>
/// Реализация сервиса для управления корзиной покупок
/// Использует Preferences для локального хранения данных
/// </summary>
public class CartService : ICartService
{
    private const string CartItemsKey = "cart_items";
    private readonly ILogger<CartService>? _logger;

    public CartService(ILogger<CartService>? logger = null)
    {
        _logger = logger;
    }

    public async Task<List<CartItem>> GetCartItemsAsync(CancellationToken ct = default)
    {
        try
        {
            var cartJson = Preferences.Get(CartItemsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(cartJson))
            {
                return new List<CartItem>();
            }

            var items = JsonSerializer.Deserialize<List<CartItem>>(cartJson);
            return items ?? new List<CartItem>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting cart items");
            return new List<CartItem>();
        }
    }

    public async Task<Dictionary<int, List<CartItem>>> GetCartItemsByPartnerAsync(CancellationToken ct = default)
    {
        var items = await GetCartItemsAsync(ct);
        return items
            .GroupBy(item => item.PartnerId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task AddToCartAsync(ProductDto product, int partnerId, string partnerName, string? partnerLogoUrl, CancellationToken ct = default)
    {
        try
        {
            var items = await GetCartItemsAsync(ct);
            
            // Проверяем, есть ли уже такой товар в корзине
            var existingItem = items.FirstOrDefault(item => item.ProductId == product.Id);
            
            if (existingItem != null)
            {
                // Увеличиваем количество
                existingItem.Quantity++;
            }
            else
            {
                // Добавляем новый товар
                var cartItem = new CartItem
                {
                    ProductId = product.Id,
                    PartnerId = partnerId,
                    PartnerName = partnerName,
                    PartnerLogoUrl = partnerLogoUrl,
                    ProductName = product.Name,
                    ProductDescription = product.Description,
                    ProductImageUrl = product.ImageUrl,
                    Price = product.Price,
                    OriginalPrice = product.OriginalPrice,
                    DiscountPercent = product.DiscountPercent,
                    YessCoins = product.YessCoins,
                    Quantity = 1
                };
                
                items.Add(cartItem);
            }

            await SaveCartItemsAsync(items, ct);
            _logger?.LogDebug("Added product {ProductId} to cart", product.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding product {ProductId} to cart", product.Id);
            throw;
        }
    }

    public async Task RemoveFromCartAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            var items = await GetCartItemsAsync(ct);
            items.RemoveAll(item => item.ProductId == productId);
            await SaveCartItemsAsync(items, ct);
            _logger?.LogDebug("Removed product {ProductId} from cart", productId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing product {ProductId} from cart", productId);
            throw;
        }
    }

    public async Task UpdateQuantityAsync(int productId, int quantity, CancellationToken ct = default)
    {
        try
        {
            if (quantity <= 0)
            {
                await RemoveFromCartAsync(productId, ct);
                return;
            }

            var items = await GetCartItemsAsync(ct);
            var item = items.FirstOrDefault(i => i.ProductId == productId);
            
            if (item != null)
            {
                item.Quantity = quantity;
                await SaveCartItemsAsync(items, ct);
                _logger?.LogDebug("Updated quantity for product {ProductId} to {Quantity}", productId, quantity);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating quantity for product {ProductId}", productId);
            throw;
        }
    }

    public async Task ClearCartAsync(CancellationToken ct = default)
    {
        try
        {
            Preferences.Remove(CartItemsKey);
            _logger?.LogDebug("Cart cleared");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing cart");
            throw;
        }
    }

    public async Task<int> GetTotalItemsCountAsync(CancellationToken ct = default)
    {
        var items = await GetCartItemsAsync(ct);
        return items.Sum(item => item.Quantity);
    }

    public async Task<decimal> GetTotalPriceAsync(CancellationToken ct = default)
    {
        var items = await GetCartItemsAsync(ct);
        return items.Sum(item => item.TotalPrice);
    }

    private async Task SaveCartItemsAsync(List<CartItem> items, CancellationToken ct = default)
    {
        try
        {
            var cartJson = JsonSerializer.Serialize(items);
            Preferences.Set(CartItemsKey, cartJson);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving cart items");
            throw;
        }
    }
}

