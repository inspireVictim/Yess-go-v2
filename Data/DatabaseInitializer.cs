using Microsoft.Extensions.Logging;

namespace YessGoFront.Data;

/// <summary>
/// Сервис для инициализации базы данных
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializer>? _logger;

    public DatabaseInitializer(
        AppDbContext context,
        ILogger<DatabaseInitializer>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    /// <summary>
    /// Инициализировать базу данных (создать таблицы если их нет)
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger?.LogInformation("Initializing database...");
            
            // Для SQLite в мобильном приложении всегда используем EnsureCreatedAsync
            // Это создаст таблицы если их нет, или ничего не сделает если они уже есть
            await _context.Database.EnsureCreatedAsync();
            
            _logger?.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing database");
            // Повторная попытка
            try
            {
                _logger?.LogWarning("Retrying database initialization...");
                await _context.Database.EnsureCreatedAsync();
                _logger?.LogInformation("Database created successfully using retry");
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogError(fallbackEx, "Failed to initialize database even with retry");
                throw;
            }
        }
    }

    /// <summary>
    /// Заполнить базу данных начальными данными (seed data)
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding database...");
            
            // TODO: Добавьте код для заполнения начальными данными
            // Например:
            // if (!await _context.Users.AnyAsync())
            // {
            //     _context.Users.AddRange(GetSeedUsers());
            //     await _context.SaveChangesAsync();
            // }
            
            _logger?.LogInformation("Database seeded successfully");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding database");
            throw;
        }
    }

    /// <summary>
    /// Сбросить базу данных (удалить и создать заново)
    /// </summary>
    public async Task ResetAsync()
    {
        try
        {
            _logger?.LogWarning("Resetting database...");
            
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            
            _logger?.LogInformation("Database reset successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error resetting database");
            throw;
        }
    }
}

