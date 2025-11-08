using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;
using YessGoFront.Models;

namespace YessGoFront.Services.Api;

public interface IBannerApiService
{
    Task<List<BannerDto>> GetBannersAsync(CancellationToken ct = default);
    Task<List<BannerDto>> GetActiveBannersAsync(CancellationToken ct = default);
}

public class BannerApiService : ApiClient, IBannerApiService
{
    public BannerApiService(HttpClient httpClient, ILogger<BannerApiService>? logger = null)
        : base(httpClient, logger)
    {
    }

    public async Task<List<BannerDto>> GetBannersAsync(CancellationToken ct = default)
    {
        var response = await GetAsync<List<BannerDto>>(ApiEndpoints.BannerEndpoints.List, ct);
        return response ?? new List<BannerDto>();
    }

    public async Task<List<BannerDto>> GetActiveBannersAsync(CancellationToken ct = default)
    {
        var response = await GetAsync<List<BannerDto>>(ApiEndpoints.BannerEndpoints.Active, ct);
        return response ?? new List<BannerDto>();
    }
}

