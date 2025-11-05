using YessGoFront.Config;

namespace YessGoFront.Data;

/// <summary>
/// Сервис для получения строки подключения к базе данных
/// </summary>
public interface IDatabaseConnectionService
{
    string GetConnectionString();
    bool IsSqlLoggingEnabled();
}

/// <summary>
/// Реализация сервиса для получения строки подключения к PostgreSQL
/// </summary>
public class DatabaseConnectionService : IDatabaseConnectionService
{
    private readonly AppSettings _settings;

    public DatabaseConnectionService(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public string GetConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_settings.Database.ConnectionString))
        {
            throw new InvalidOperationException(
                "Database connection string is not configured. " +
                "Please set AppSettings.Database.ConnectionString in MauiProgram.cs");
        }

        return _settings.Database.ConnectionString;
    }

    public bool IsSqlLoggingEnabled()
    {
        return _settings.Database.EnableSqlLogging;
    }
}

