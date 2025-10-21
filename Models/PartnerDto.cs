using System.Text.Json.Serialization;

namespace YessGoFront.Models;

public class PartnerDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("subTitle")] public string? SubTitle { get; set; }
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
    [JsonPropertyName("logoUrl")] public string? LogoUrl { get; set; }
    [JsonPropertyName("cashbackPercent")] public double CashbackPercent { get; set; }
}
