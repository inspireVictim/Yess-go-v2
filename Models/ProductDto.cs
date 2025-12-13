using System.Text.Json.Serialization;

namespace YessGoFront.Models;

/// <summary>
/// Обёртка для пагинированного ответа API с товарами
/// </summary>
public class PagedProductsResponse
{
    [JsonPropertyName("items")]
    public List<ProductDto> Items { get; set; } = new();
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}

/// <summary>
/// DTO для продукта партнёра
/// </summary>
public class ProductDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("partner_id")]
    public int PartnerId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("ingredients")]
    public string? Ingredients { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("weight")]
    public string? Weight { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("original_price")]
    public decimal? OriginalPrice { get; set; }

    [JsonPropertyName("discount_percent")]
    public decimal? DiscountPercent { get; set; }

    [JsonPropertyName("yess_coins")]
    public decimal? YessCoins { get; set; }

    [JsonPropertyName("is_available")]
    public bool IsAvailable { get; set; } = true;

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Эффективное количество Yess!Coins для отображения
    /// Использует значение из БД, если оно задано и > 0,
    /// иначе рассчитывает как разницу между OriginalPrice и Price
    /// </summary>
    public decimal EffectiveYessCoins
    {
        get
        {
            // Если YessCoins из БД задан и > 0, используем его
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
