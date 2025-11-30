using YessGoFront.Models;

namespace YessGoFront.Services.Domain;

/// <summary>
/// Сервис для управления корзиной покупок
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Получить все элементы корзины
    /// </summary>
    Task<List<CartItem>> GetCartItemsAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить элементы корзины, сгруппированные по партнёрам
    /// </summary>
    Task<Dictionary<int, List<CartItem>>> GetCartItemsByPartnerAsync(CancellationToken ct = default);

    /// <summary>
    /// Добавить товар в корзину
    /// </summary>
    Task AddToCartAsync(ProductDto product, int partnerId, string partnerName, string? partnerLogoUrl, CancellationToken ct = default);

    /// <summary>
    /// Удалить товар из корзины
    /// </summary>
    Task RemoveFromCartAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Изменить количество товара в корзине
    /// </summary>
    Task UpdateQuantityAsync(int productId, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Очистить корзину
    /// </summary>
    Task ClearCartAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить общее количество товаров в корзине
    /// </summary>
    Task<int> GetTotalItemsCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить общую стоимость корзины
    /// </summary>
    Task<decimal> GetTotalPriceAsync(CancellationToken ct = default);
}

