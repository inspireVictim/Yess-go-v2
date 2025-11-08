using System.Text.Json.Serialization;

namespace YessGoFront.Models;

public class BannerDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("partner_id")]
    public int? PartnerId { get; set; }
    
    [JsonPropertyName("partner_name")]
    public string? PartnerName { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
    
    [JsonPropertyName("order")]
    public int Order { get; set; } = 0;
}

