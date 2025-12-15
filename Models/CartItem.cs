using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YessGoFront.Models;

/// <summary>
/// Элемент корзины
/// </summary>
public partial class CartItem : ObservableObject
{
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    [JsonPropertyName("partner_id")]
    public int PartnerId { get; set; }

    [JsonPropertyName("partner_name")]
    public string PartnerName { get; set; } = string.Empty;

    [JsonPropertyName("partner_logo_url")]
    public string? PartnerLogoUrl { get; set; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("product_description")]
    public string? ProductDescription { get; set; }

    [JsonPropertyName("product_image_url")]
    public string? ProductImageUrl { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("original_price")]
    public decimal? OriginalPrice { get; set; }

    [JsonPropertyName("discount_percent")]
    public decimal? DiscountPercent { get; set; }

    [JsonPropertyName("yess_coins")]
    public decimal? YessCoins { get; set; }

    private int _quantity = 1;

    /// <summary>
    /// Количество товара (уведомляет UI об изменениях)
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                // Уведомляем об изменении вычисляемых свойств
                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(TotalYessCoins));
            }
        }
    }

    /// <summary>
    /// Общая цена за все единицы товара
    /// </summary>
    public decimal TotalPrice => Price * Quantity;

    /// <summary>
    /// Общее количество Yess!Coins за все единицы товара
    /// </summary>
    public decimal TotalYessCoins => (YessCoins ?? 0) * Quantity;

    /// <summary>
    /// Процент скидки для отображения
    /// </summary>
    public string DiscountPercentText => DiscountPercent.HasValue 
        ? $"-{DiscountPercent.Value:F0}%" 
        : string.Empty;

    /// <summary>
    /// Эффективное количество Yess!Coins для отображения
    /// Использует значение из YessCoins, если оно задано и > 0,
    /// иначе рассчитывает как разницу между OriginalPrice и Price
    /// </summary>
    public decimal EffectiveYessCoins
    {
        get
        {
            // Если YessCoins задан и > 0, используем его
            if (YessCoins.HasValue && YessCoins.Value > 0)
            {
                return YessCoins.Value;
            }

            // Иначе рассчитываем как разницу между OriginalPrice и Price
            if (OriginalPrice.HasValue && OriginalPrice.Value > Price)
            {
                return OriginalPrice.Value - Price;
            }

            return 0;
        }
    }
}

