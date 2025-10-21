using System.Net.Http.Json;
using YessGoFront.Models;

namespace YessGoFront.Services;

public interface IPartnersService
{
    Task<IReadOnlyList<PartnerDto>> GetByCategoryAsync(string category, CancellationToken ct = default);
}

public sealed class PartnersService : IPartnersService
{
    private readonly HttpClient _http;

    public PartnersService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient
        {
            BaseAddress = new Uri("https://your-api.example.com")
        };
    }

    public async Task<IReadOnlyList<PartnerDto>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        try
        {
            var data = await _http.GetFromJsonAsync<List<PartnerDto>>(
                $"/api/partners?category={Uri.EscapeDataString(category)}", ct);
            return data ?? new();
        }
        catch
        {
            // Мок для теста
            return new List<PartnerDto>
            {
                new PartnerDto { Id = "1", Name = "Home Market", SubTitle = "Мебель", Category = category, LogoUrl = "partner_home_1", CashbackPercent = 5 },
                new PartnerDto { Id = "2", Name = "Soft Room", SubTitle = "Интерьер", Category = category, LogoUrl = "partner_home_2", CashbackPercent = 7 }
            };
        }
    }
}
