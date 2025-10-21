using System.Text.Json.Serialization;

namespace YessGoFront.Models;

public class UserDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("fullName")] public string? FullName { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("avatarUrl")] public string? AvatarUrl { get; set; }
}
