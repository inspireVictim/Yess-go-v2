namespace YessGoFront.Config;

/// <summary>
/// Настройки приложения (API, таймауты и т.д.)
/// </summary>
public class AppSettings
{
    public ApiSettings Api { get; set; } = new();
    public TimeoutSettings Timeouts { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
}

public class DatabaseSettings
{
    /// <summary>
    /// Строка подключения к PostgreSQL
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Включить логирование SQL запросов (для Debug режима)
    /// </summary>
    public bool EnableSqlLogging { get; set; } = false;
}

public class ApiSettings
{
    /// <summary>
    /// Базовый URL API (будет установлен после клонирования репозитория бэкенда)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.yessgo.com";
    
    /// <summary>
    /// Версия API
    /// </summary>
    public string ApiVersion { get; set; } = "v1";
    
    /// <summary>
    /// Таймаут запросов в секундах
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}

public class TimeoutSettings
{
    public int RequestTimeout { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

