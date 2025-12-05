namespace YessGoFront.Services;

/// <summary>
/// Интерфейс для работы с Finik Payment SDK
/// </summary>
public interface IFinikPaymentService
{
    /// <summary>
    /// Запускает процесс оплаты через Finik SDK
    /// </summary>
    /// <param name="request">Параметры платежного запроса</param>
    /// <returns>Результат платежа</returns>
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
}

/// <summary>
/// Параметры платежного запроса для Finik SDK
/// </summary>
public class PaymentRequest
{
    /// <summary>
    /// Сумма платежа
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Описание товара/услуги
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Название товара (на английском)
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Уникальный ID запроса (для предотвращения дубликатов)
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Дополнительные поля, которые будут возвращены в callback
    /// </summary>
    public Dictionary<string, string>? RequiredFields { get; set; }

    /// <summary>
    /// Максимальное количество раз, которое товар может быть приобретен
    /// </summary>
    public int? MaxAvailableQuantity { get; set; }
}

/// <summary>
/// Результат платежа через Finik SDK
/// </summary>
public class PaymentResult
{
    /// <summary>
    /// Успешно ли выполнен платеж
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Статус платежа (SUCCEEDED, FAILED и т.д.)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Сумма платежа
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// ID транзакции
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Дата транзакции в миллисекундах
    /// </summary>
    public long? TransactionDate { get; set; }

    /// <summary>
    /// Сообщение об ошибке (если есть)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Дополнительные поля, переданные в RequiredFields
    /// </summary>
    public Dictionary<string, string>? Fields { get; set; }

    /// <summary>
    /// Пользователь отменил платеж
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// JSON результат платежа (полный ответ от SDK)
    /// </summary>
    public string? PaymentResultJson { get; set; }
}

