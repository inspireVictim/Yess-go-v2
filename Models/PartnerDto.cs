using System.Text.Json.Serialization;

namespace YessGoFront.Models;

public class PartnerDto
{
    [JsonPropertyName("id")] 
    public int Id { get; set; }
    
    [JsonPropertyName("name")] 
    public string? Name { get; set; }
    
    [JsonPropertyName("subTitle")] 
    public string? SubTitle { get; set; }
    
    [JsonPropertyName("category")] 
    public string? Category { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("logo_url")] 
    public string? LogoUrl { get; set; }
    
    [JsonPropertyName("default_cashback_rate")] 
    public double CashbackPercent { get; set; }
    
    [JsonPropertyName("categories")]
    public List<CategoryDto>? Categories { get; set; }
}
