namespace YessGoFront.Config;

/// <summary>
/// Конфигурация для Finik Payment SDK
/// ВАЖНО: Эти значения временно захардкожены для тестирования.
/// В будущем их следует получать с бэкенда или из SecureStorage.
/// </summary>
public static class FinikConfig
{
    /// <summary>
    /// API ключ от Finik (получается у представителей Finik)
    /// </summary>
    public const string ApiKey = "YOUR_API_KEY_HERE";

    /// <summary>
    /// Account ID бенефициара (получается у представителей Finik)
    /// </summary>
    public const string AccountId = "YOUR_ACCOUNT_ID_HERE";

    /// <summary>
    /// URL для callback/webhook (Finik отправит сюда POST запрос с результатом платежа)
    /// </summary>
    public const string CallbackUrl = "https://your-backend-url.kg/api/finik/callback";

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

