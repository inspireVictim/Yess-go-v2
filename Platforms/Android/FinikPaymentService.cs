#if ANDROID
using System;
using System.Threading.Tasks;
using YessGoFront.Services;
using YessGoFront.Services.Api;
using Debug = System.Diagnostics.Debug;

namespace YessGoFront.Platforms.Android;

/// <summary>
/// Реализация Finik Payment Service для Android через Backend Proxy
/// Все вызовы Finik SDK выполняются на backend, мобильное приложение использует REST API
/// </summary>
public class FinikPaymentService : IFinikPaymentService
{
    private readonly IPaymentApiService _paymentApiService;

    public FinikPaymentService(IPaymentApiService paymentApiService)
    {
        _paymentApiService = paymentApiService ?? throw new ArgumentNullException(nameof(paymentApiService));
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            Debug.WriteLine($"[FinikPaymentService] Starting payment via backend API: Amount={request.Amount}, Description={request.Description}");

            // Создаем запрос для backend API
            var apiRequest = new CreateFinikPaymentRequest
            {
                Amount = request.Amount,
                Description = request.Description,
                NameEn = request.NameEn,
                RequestId = request.RequestId ?? Guid.NewGuid().ToString(),
                RequiredFields = request.RequiredFields,
                MaxAvailableQuantity = request.MaxAvailableQuantity
            };

            // Вызываем backend API для создания платежа
            var apiResponse = await _paymentApiService.CreateFinikPaymentAsync(apiRequest);

            Debug.WriteLine($"[FinikPaymentService] Payment created: PaymentId={apiResponse.PaymentId}, Status={apiResponse.Status}");

            // Преобразуем ответ API в PaymentResult
            var result = new PaymentResult
            {
                IsSuccess = apiResponse.Status == "completed" || apiResponse.Status == "succeeded",
                Status = apiResponse.Status,
                Amount = apiResponse.Amount ?? request.Amount,
                TransactionId = apiResponse.TransactionId,
                ErrorMessage = apiResponse.ErrorMessage,
                IsCancelled = apiResponse.Status == "cancelled"
            };

            // Если платеж в статусе pending, можно опционально проверить статус через polling
            if (apiResponse.Status == "pending" && !string.IsNullOrEmpty(apiResponse.PaymentId))
            {
                Debug.WriteLine($"[FinikPaymentService] Payment is pending, PaymentId={apiResponse.PaymentId}");
                // Опционально: можно добавить polling для проверки статуса
                // result = await PollPaymentStatusAsync(apiResponse.PaymentId);
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FinikPaymentService] Error processing payment: {ex.Message}");
            Debug.WriteLine($"[FinikPaymentService] StackTrace: {ex.StackTrace}");

            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Ошибка при создании платежа: {ex.Message}",
                IsCancelled = false
            };
        }
    }

    /// <summary>
    /// Опциональный метод для проверки статуса платежа через polling
    /// </summary>
    private async Task<PaymentResult> PollPaymentStatusAsync(string paymentId, int maxAttempts = 10, int delayMs = 2000)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            await Task.Delay(delayMs);

            try
            {
                var statusResponse = await _paymentApiService.GetPaymentStatusAsync(paymentId);

                if (statusResponse.Status == "completed" || statusResponse.Status == "succeeded")
                {
                    return new PaymentResult
                    {
                        IsSuccess = true,
                        Status = statusResponse.Status,
                        Amount = statusResponse.Amount,
                        TransactionId = statusResponse.TransactionId
                    };
                }

                if (statusResponse.Status == "failed" || statusResponse.Status == "cancelled")
                {
                    return new PaymentResult
                    {
                        IsSuccess = false,
                        Status = statusResponse.Status,
                        ErrorMessage = statusResponse.ErrorMessage,
                        IsCancelled = statusResponse.Status == "cancelled"
                    };
                }

                // Если все еще pending, продолжаем polling
                Debug.WriteLine($"[FinikPaymentService] Payment still pending, attempt {attempt + 1}/{maxAttempts}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FinikPaymentService] Error polling payment status: {ex.Message}");
            }
        }

        // Если после всех попыток статус не изменился
        return new PaymentResult
        {
            IsSuccess = false,
            Status = "pending",
            ErrorMessage = "Не удалось получить финальный статус платежа"
        };
    }
}
#endif
