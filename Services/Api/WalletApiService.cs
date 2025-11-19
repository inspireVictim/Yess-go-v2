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
        Logger?.LogDebug("Запрос баланса через endpoint: {Endpoint}", ApiEndpoints.WalletEndpoints.Balance);
        var response = await GetAsync<BalanceResponse>(ApiEndpoints.WalletEndpoints.Balance, ct);
        Logger?.LogInformation("Получен баланс: {Balance} {Currency}", response.Balance, response.Currency ?? "KGS");
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
        [System.Text.Json.Serialization.JsonPropertyName("balance")]
        public decimal Balance { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("currency")]
        public string? Currency { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("last_updated")]
        public DateTime? LastUpdated { get; set; }
    }
}

