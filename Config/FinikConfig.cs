namespace YessGoFront.Config;

/// <summary>
/// Конфигурация для Finik Payment SDK
/// ВАЖНО: Вся логика работы с Finik API реализована на бэкенде (Django микросервис).
/// Фронтенд только передает данные (user_id, customer_name, amount) на бэкенд.
/// </summary>
public static class FinikConfig
{
    /// <summary>
    /// Использовать beta сервер (true для тестирования, false для production)
    /// </summary>
    public const bool IsBeta = true;

    /// <summary>
    /// Локализация интерфейса (KY, EN, RU)
    /// </summary>
    public const string Locale = "RU";

    /// <summary>
    /// Сценарий текста: PAYMENT или REPLENISHMENT
    /// </summary>
    public const string TextScenario = "REPLENISHMENT";

    /// <summary>
    /// Разрешить кнопку поделиться
    /// </summary>
    public const bool EnableShare = true;

    /// <summary>
    /// Сделать кнопки поддержки кликабельными
    /// </summary>
    public const bool TapableSupportButtons = true;
}

