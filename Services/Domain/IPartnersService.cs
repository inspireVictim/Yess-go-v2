using YessGoFront.Models;

namespace YessGoFront.Services.Domain;

/// <summary>
/// Domain сервис для работы с партнёрами (бизнес-логика)
/// </summary>
public interface IPartnersService
{
    Task<IReadOnlyList<PartnerDto>> GetPartnersByCategoryAsync(
        string category,
        CancellationToken ct = default);

    Task<PartnerDetailDto> GetPartnerByIdAsync(
        string id,
        CancellationToken ct = default);

    Task<IReadOnlyList<PartnerDto>> SearchPartnersAsync(
        string query,
        CancellationToken ct = default);

    Task<IReadOnlyList<PartnerDto>> GetNearbyPartnersAsync(
        double latitude,
        double longitude,
        int radius = 5000,
        CancellationToken ct = default);
}

