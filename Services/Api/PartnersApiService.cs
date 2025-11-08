using Microsoft.Extensions.Logging;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;
using YessGoFront.Models;

namespace YessGoFront.Services.Api;

/// <summary>
/// Реализация API сервиса для работы с партнёрами
/// </summary>
public class PartnersApiService : ApiClient, IPartnersApiService
{
    public PartnersApiService(
        HttpClient httpClient,
        ILogger<PartnersApiService>? logger = null)
        : base(httpClient, logger)
    {
    }

    public async Task<IReadOnlyList<PartnerDto>> GetAllAsync(
        CancellationToken ct = default)
    {
        var endpoint = ApiEndpoints.PartnersEndpoints.List;
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        return result ?? new List<PartnerDto>();
    }

    public async Task<IReadOnlyList<PartnerDto>> GetByCategoryAsync(
        string category,
        CancellationToken ct = default)
    {
        var endpoint = ApiEndpoints.PartnersEndpoints.ByCategory(category);
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        return result ?? new List<PartnerDto>();
    }

    public async Task<PartnerDetailDto> GetByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        if (!int.TryParse(id, out var partnerId))
        {
            throw new ArgumentException($"Invalid partner ID: {id}", nameof(id));
        }
        var endpoint = ApiEndpoints.PartnersEndpoints.ById(partnerId);
        return await GetAsync<PartnerDetailDto>(endpoint, ct);
    }

    public async Task<IReadOnlyList<PartnerDto>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        var endpoint = $"{ApiEndpoints.PartnersEndpoints.List}?query={Uri.EscapeDataString(query)}";
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        return result ?? new List<PartnerDto>();
    }

    public async Task<IReadOnlyList<PartnerDto>> GetNearbyAsync(
        double latitude,
        double longitude,
        int radius = 5000,
        CancellationToken ct = default)
    {
        var endpoint = ApiEndpoints.PartnersEndpoints.Nearby(latitude, longitude, radius / 1000.0); // Преобразуем радиус из метров в километры
        var result = await GetAsync<List<PartnerDto>>(endpoint, ct);
        return result ?? new List<PartnerDto>();
    }
}

