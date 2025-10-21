using System.Text.Json.Serialization;

namespace YessGoFront.Models;

/// <summary>Подробности партнёра для экрана карточки.</summary>
public class PartnerDetailDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("logoUrl")] public string? LogoUrl { get; set; }
    [JsonPropertyName("bannerUrl")] public string? BannerUrl { get; set; }

    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("latitude")] public double? Latitude { get; set; }
    [JsonPropertyName("longitude")] public double? Longitude { get; set; }

    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("website")] public string? Website { get; set; }

    [JsonPropertyName("rating")] public double? Rating { get; set; }
    [JsonPropertyName("reviewsCount")] public int? ReviewsCount { get; set; }

    [JsonPropertyName("cashbackPercent")]
    public double CashbackPercent { get; set; }

    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
}
