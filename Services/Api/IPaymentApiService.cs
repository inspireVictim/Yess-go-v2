namespace YessGoFront.Services.Api;

/// <summary>
/// API сервис для работы с платежами через Finik Web SDK (через backend)
/// </summary>
public interface IPaymentApiService
{
    /// <summary>
    /// Создает платеж через Finik Web SDK на backend
    /// </summary>
    /// <param name="amount">Сумма платежа</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>URL страницы оплаты Finik</returns>
    Task<CreatePaymentResponse> CreatePaymentAsync(decimal amount, CancellationToken ct = default);

    /// <summary>
    /// Получает статус платежа по ID
    /// </summary>
    /// <param name="paymentId">ID платежа</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Статус платежа</returns>
    Task<PaymentStatusResponse> GetPaymentStatusAsync(string paymentId, CancellationToken ct = default);
}

/// <summary>
/// Ответ на создание платежа через Finik Web SDK
/// </summary>
public class CreatePaymentResponse
{
    /// <summary>
    /// URL страницы оплаты Finik для отображения в WebView
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL для редиректа после успешной оплаты (для отслеживания завершения оплаты)
    /// </summary>
    public string? RedirectUrl { get; set; }
}

/// <summary>
/// Ответ на запрос статуса платежа
/// </summary>
public class PaymentStatusResponse
{
    /// <summary>
    /// ID платежа
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Статус платежа (pending, completed, failed)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// ID транзакции от Finik
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Сумма платежа
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Сообщение об ошибке (если есть)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Webhook payload от Finik (для использования на backend)
/// Используется для обработки callback от Finik после успешного платежа
/// </summary>
public class FinikWebhookPayload
{
    /// <summary>
    /// ID транзакции
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID аккаунта
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("accountId")]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Сумма платежа
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Дополнительные поля, переданные в requiredFields
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public Dictionary<string, string>? Fields { get; set; }

    /// <summary>
    /// Информация о товаре
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("item")]
    public FinikWebhookItem? Item { get; set; }

    /// <summary>
    /// Чистая сумма (после комиссий)
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("net")]
    public decimal? Net { get; set; }

    /// <summary>
    /// Номер чека
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("receiptNumber")]
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Дата запроса (timestamp в миллисекундах)
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("requestDate")]
    public long? RequestDate { get; set; }

    /// <summary>
    /// Информация о сервисе
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("service")]
    public FinikWebhookService? Service { get; set; }

    /// <summary>
    /// Статус платежа (SUCCEEDED, FAILED)
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Дата транзакции (timestamp в миллисекундах)
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("transactionDate")]
    public long? TransactionDate { get; set; }

    /// <summary>
    /// ID транзакции
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Тип транзакции (DEBIT, CREDIT)
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("transactionType")]
    public string? TransactionType { get; set; }

    /// <summary>
    /// Дополнительные данные (только для WEB)
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public Dictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Информация о товаре в webhook payload
/// </summary>
public class FinikWebhookItem
{
    /// <summary>
    /// ID товара
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Информация о сервисе в webhook payload
/// </summary>
public class FinikWebhookService
{
    /// <summary>
    /// ID сервиса
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

