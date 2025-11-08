using System.Text.Json.Serialization;

namespace YessGoFront.Models;

public class PartnerDto
{
    [JsonPropertyName("id")] 
    public int Id { get; set; }
    
    [JsonPropertyName("name")] 
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("subTitle")] 
    public string? SubTitle { get; set; }
    
    [JsonPropertyName("category")] 
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("logo_url")] 
    public string? LogoUrl { get; set; }
    
    [JsonPropertyName("default_cashback_rate")] 
    public double CashbackPercent { get; set; }
}
