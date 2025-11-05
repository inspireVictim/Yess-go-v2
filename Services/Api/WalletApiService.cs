using Microsoft.Extensions.Logging;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;
using YessGoFront.Models;

namespace YessGoFront.Services.Api;

/// <summary>
/// Реализация API сервиса для работы с кошельком
/// </summary>
public class WalletApiService : ApiClient, IWalletApiService
{
    public WalletApiService(
        HttpClient httpClient,
        ILogger<WalletApiService>? logger = null)
        : base(httpClient, logger)
    {
    }

    public async Task<decimal> GetBalanceAsync(CancellationToken ct = default)
    {
        var response = await GetAsync<BalanceResponse>(ApiEndpoints.WalletEndpoints.Balance, ct);
        return response.Balance;
    }

    public async Task<IReadOnlyList<PurchaseDto>> GetHistoryAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var endpoint = $"{ApiEndpoints.TransactionEndpoints.List}?page={page}&pageSize={pageSize}";
        var result = await GetAsync<List<PurchaseDto>>(endpoint, ct);
        return result ?? new List<PurchaseDto>();
    }

    public async Task<PurchaseDto> GetTransactionByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        var endpoint = ApiEndpoints.TransactionEndpoints.ById(id);
        return await GetAsync<PurchaseDto>(endpoint, ct);
    }

    private class BalanceResponse
    {
        public decimal Balance { get; set; }
    }
}

