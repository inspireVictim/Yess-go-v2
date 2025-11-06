using System.Text.Json.Serialization;

namespace YessGoFront.Models;

/// <summary>
/// DTO пользователя, соответствует UserResponse схеме бэкенда
/// </summary>
public class UserDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("phone_number")]
    public string Phone { get; set; } = string.Empty;
    
    [JsonPropertyName("city_id")]
    public int? CityId { get; set; }
    
    [JsonPropertyName("referral_code")]
    public string? ReferralCode { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Полное имя для отображения
    /// </summary>
    public string DisplayName
    {
        get
        {
            var fullName = $"{FirstName} {LastName}".Trim();
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : Phone;
        }
    }
}
