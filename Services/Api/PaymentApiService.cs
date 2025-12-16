using Microsoft.Extensions.Logging;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;

namespace YessGoFront.Services.Api;

/// <summary>
/// Реализация API сервиса для работы с платежами через Finik Web SDK (через backend)
/// </summary>
public class PaymentApiService : ApiClient, IPaymentApiService
{
    public PaymentApiService(
        HttpClient httpClient,
        ILogger<PaymentApiService>? logger = null)
        : base(httpClient, logger)
    {
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(
        decimal amount,
        CancellationToken ct = default)
    {
        Logger?.LogInformation("Создание платежа через Finik Web SDK: Amount={Amount}", amount);

        try
        {
            // Округляем сумму до 2 знаков после запятой для корректной отправки
            var roundedAmount = Math.Round(amount, 2);
            
            // Создаем запрос с правильным форматом для Django бэкенда
            // Django бэкенд ожидает только поле "amount"
            var request = new 
            { 
                amount = roundedAmount
            };
            
            Logger?.LogDebug("Отправка запроса на создание платежа: Amount={Amount}, Endpoint={Endpoint}", 
                roundedAmount, ApiEndpoints.PaymentEndpoints.Create);
            
            var endpoint = ApiEndpoints.PaymentEndpoints.Create;
            var response = await PostAsync<object, CreatePaymentResponse>(
                endpoint, request, ct);

            if (response == null)
            {
                Logger?.LogError("Получен пустой ответ от сервера при создании платежа");
                throw new InvalidOperationException("Получен пустой ответ от сервера");
            }

            Logger?.LogInformation("Платеж создан успешно: PaymentUrl={PaymentUrl}, RedirectUrl={RedirectUrl}", 
                response.PaymentUrl, response.RedirectUrl ?? "не указан");

            return response;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Ошибка при создании платежа: Amount={Amount}, Error={Error}", 
                amount, ex.Message);
            throw;
        }
    }

    public async Task<PaymentStatusResponse> GetPaymentStatusAsync(
        string paymentId,
        CancellationToken ct = default)
    {
        Logger?.LogDebug("Запрос статуса платежа: PaymentId={PaymentId}", paymentId);

        var endpoint = ApiEndpoints.PaymentEndpoints.GetPaymentStatus(paymentId);
        var response = await GetAsync<PaymentStatusResponse>(endpoint, ct);

        Logger?.LogInformation(
            "Статус платежа: PaymentId={PaymentId}, Status={Status}, TransactionId={TransactionId}",
            response.PaymentId, response.Status, response.TransactionId);

        return response;
    }
}

