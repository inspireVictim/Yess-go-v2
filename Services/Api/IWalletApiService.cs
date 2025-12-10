using YessGoFront.Models;

namespace YessGoFront.Services.Api;

/// <summary>
/// API сервис для работы с кошельком
/// </summary>
public interface IWalletApiService
{
    /// <summary>
    /// Получить баланс
    /// </summary>
    Task<decimal> GetBalanceAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить историю транзакций
    /// </summary>
    Task<IReadOnlyList<PurchaseDto>> GetHistoryAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Получить транзакцию по ID
    /// </summary>
    Task<PurchaseDto> GetTransactionByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Перевести средства партнеру
    /// </summary>
    Task<PurchaseDto> TransferToPartnerAsync(int partnerId, decimal amount, string? description = null, CancellationToken ct = default);
}

