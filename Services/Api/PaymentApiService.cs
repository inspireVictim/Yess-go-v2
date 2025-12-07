using Microsoft.Extensions.Logging;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;

namespace YessGoFront.Services.Api;

/// <summary>
/// Реализация API сервиса для работы с платежами через Finik SDK (через backend)
/// </summary>
public class PaymentApiService : ApiClient, IPaymentApiService
{
    public PaymentApiService(
        HttpClient httpClient,
        ILogger<PaymentApiService>? logger = null)
        : base(httpClient, logger)
    {
    }

    public async Task<CreateFinikPaymentResponse> CreateFinikPaymentAsync(
        CreateFinikPaymentRequest request,
        CancellationToken ct = default)
    {
        Logger?.LogInformation(
            "Создание платежа через Finik SDK: Amount={Amount}, Description={Description}, RequestId={RequestId}",
            request.Amount, request.Description, request.RequestId);

        var endpoint = ApiEndpoints.PaymentEndpoints.CreateFinikPayment;
        var response = await PostAsync<CreateFinikPaymentRequest, CreateFinikPaymentResponse>(
            endpoint, request, ct);

        Logger?.LogInformation(
            "Платеж создан: PaymentId={PaymentId}, Status={Status}, TransactionId={TransactionId}",
            response.PaymentId, response.Status, response.TransactionId);

        return response;
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

