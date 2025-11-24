using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YessGoFront.Data;
using YessGoFront.Data.Entities;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Models;
using YessGoFront.Services.Api;

namespace YessGoFront.Services.Domain;

/// <summary>
/// Реализация Domain сервиса для работы с кошельком
/// </summary>
public class WalletService : IWalletService
{
    private readonly IWalletApiService _apiService;
    private readonly IAuthService _authService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WalletService>? _logger;

    public WalletService(
        IWalletApiService apiService,
        IAuthService authService,
        AppDbContext dbContext,
        ILogger<WalletService>? logger = null)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<decimal> GetBalanceAsync(CancellationToken ct = default)
    {
        try
        {
            // Если пользователь не аутентифицирован — не выполняем защищённый запрос
            if (!await _authService.IsAuthenticatedAsync())
            {
                _logger?.LogWarning("GetBalanceAsync called while user is not authenticated. Skipping API call.");
                return 0m;
            }

            _logger?.LogDebug("Getting wallet balance");
            return await _apiService.GetBalanceAsync(ct);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting balance");
            throw new NetworkException("Не удалось получить баланс", ex);
        }
    }

    public async Task<IReadOnlyList<PurchaseDto>> GetTransactionHistoryAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            // Получаем ID текущего пользователя
            var userId = await _authService.GetCurrentUserIdAsync();
            if (!userId.HasValue)
            {
                _logger?.LogWarning("GetTransactionHistoryAsync called while user is not authenticated. Skipping.");
                return Array.Empty<PurchaseDto>();
            }

            // Пытаемся получить транзакции из локальной БД
            var dbTransactions = await _dbContext.Transactions
                .Include(t => t.Partner)
                .Where(t => t.UserId == userId.Value)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            if (dbTransactions.Any())
            {
                _logger?.LogDebug("Found {Count} transactions in local database", dbTransactions.Count);
                return dbTransactions.Select(ConvertToPurchaseDto).ToList();
            }

            // Если в БД нет транзакций, пытаемся получить из API
            _logger?.LogDebug("No transactions in local DB, trying API, page: {Page}, pageSize: {PageSize}", 
                page, pageSize);
            return await _apiService.GetHistoryAsync(page, pageSize, ct);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting transaction history");
            throw new NetworkException("Не удалось загрузить историю транзакций", ex);
        }
    }

    private PurchaseDto ConvertToPurchaseDto(Transaction transaction)
    {
        return new PurchaseDto
        {
            Id = transaction.Id.ToString(),
            PartnerId = transaction.PartnerId?.ToString() ?? string.Empty,
            PartnerName = transaction.Partner?.Name,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Status = transaction.Status,
            CreatedAt = transaction.CreatedAt,
            DateUtc = transaction.CreatedAt,
            CashbackAmount = transaction.Type.ToLower() == "bonus" ? transaction.Amount : 0m,
            YessCoins = transaction.Type.ToLower() == "bonus" ? transaction.Amount : 0m,
            Description = GetTransactionDescription(transaction)
        };
    }

    private string? GetTransactionDescription(Transaction transaction)
    {
        return transaction.Type.ToLower() switch
        {
            "topup" => "Пополнение баланса",
            "discount" => transaction.Partner != null ? $"Скидка в {transaction.Partner.Name}" : "Скидка",
            "bonus" => transaction.Partner != null ? $"Бонус от {transaction.Partner.Name}" : "Бонус",
            "refund" => "Возврат средств",
            "payment" => transaction.Partner != null ? $"Оплата в {transaction.Partner.Name}" : "Оплата",
            _ => null
        };
    }

    public async Task<PurchaseDto> GetTransactionByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty", nameof(id));

            // Если пользователь не аутентифицирован — не выполняем защищённый запрос
            if (!await _authService.IsAuthenticatedAsync())
            {
                _logger?.LogWarning("GetTransactionByIdAsync called while user is not authenticated. Skipping API call.");
                // Возвращаем минимальный DTO без обращения к API
                return new PurchaseDto
                {
                    Id = id
                };
            }

            _logger?.LogDebug("Getting transaction by id: {Id}", id);
            return await _apiService.GetTransactionByIdAsync(id, ct);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting transaction {Id}", id);
            throw new NetworkException("Не удалось загрузить транзакцию", ex);
        }
    }
}

