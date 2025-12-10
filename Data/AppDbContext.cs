using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YessGoFront.Data.Entities;

namespace YessGoFront.Data;

/// <summary>
/// Контекст базы данных приложения
/// </summary>
public class AppDbContext : DbContext
{
    private readonly string _connectionString;
    private readonly bool _enableSqlLogging;
    private readonly ILogger<AppDbContext>? _logger;

    public AppDbContext(
        string connectionString,
        bool enableSqlLogging = false,
        ILogger<AppDbContext>? logger = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _enableSqlLogging = enableSqlLogging;
        _logger = logger;
    }

    // DbSets для сущностей
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Wallet> Wallets { get; set; } = null!;
    public DbSet<City> Cities { get; set; } = null!;
    public DbSet<Partner> Partners { get; set; } = null!;
    public DbSet<PartnerLocation> PartnerLocations { get; set; } = null!;
    public DbSet<PartnerProduct> PartnerProducts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Promotion> Promotions { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connectionString);

        // Установить NoTracking по умолчанию для read-only операций
        // Явно включать tracking только для операций записи через .AsTracking()
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        // Включить логирование SQL запросов
        if (_enableSqlLogging || 
#if DEBUG
            true
#else
            false
#endif
            )
        {
            optionsBuilder.LogTo(
                message => _logger?.LogDebug(message),
                LogLevel.Information);
        }
    }

    /// <summary>
    /// Настройка SQLite PRAGMA команд для оптимизации производительности
    /// Должен вызываться один раз при инициализации БД
    /// </summary>
    public async Task ConfigureSqlitePragmasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Используем ExecuteSqlRaw для настройки PRAGMA команд
            // Эти команды применяются один раз при первом использовании БД
            await Database.ExecuteSqlRawAsync(
                """
                PRAGMA synchronous = NORMAL;
                PRAGMA journal_mode = WAL;
                PRAGMA cache_size = -64000;
                PRAGMA temp_store = MEMORY;
                PRAGMA mmap_size = 268435456;
                PRAGMA foreign_keys = ON;
                """,
                cancellationToken);
            
            _logger?.LogDebug("SQLite PRAGMA commands configured for optimal performance");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to configure SQLite PRAGMA commands, using defaults");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Игнорируем JSON поля, которые не поддерживаются SQLite напрямую
        // Эти поля не используются в локальном кэшировании данных пользователя
        modelBuilder.Entity<Notification>()
            .Ignore(n => n.Data);

        modelBuilder.Entity<User>()
            .Ignore(u => u.DeviceTokens);

        modelBuilder.Entity<Partner>()
            .Ignore(p => p.SocialMedia);

        modelBuilder.Entity<PartnerLocation>()
            .Ignore(pl => pl.WorkingHours);

        // ============================================
        // ИНДЕКСЫ ДЛЯ ОПТИМИЗАЦИИ ПРОИЗВОДИТЕЛЬНОСТИ
        // ============================================

        // Индекс на User.Phone для быстрого поиска по телефону
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Phone)
            .HasDatabaseName("IX_Users_Phone");

        // Составной индекс на User.IsActive, IsBlocked, LastLoginAt для GetLocalUserAsync
        // Это ускорит запрос активных пользователей, отсортированных по LastLoginAt
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.IsActive, u.IsBlocked, u.LastLoginAt })
            .HasDatabaseName("IX_Users_IsActive_IsBlocked_LastLoginAt");

        // Индекс на Wallet.UserId для быстрого поиска кошелька пользователя
        modelBuilder.Entity<Wallet>()
            .HasIndex(w => w.UserId)
            .HasDatabaseName("IX_Wallets_UserId");

        // Составной индекс на Notification.UserId и CreatedAt для сортировки уведомлений
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_CreatedAt");

        // Составной индекс на Transaction.UserId и CreatedAt для истории транзакций
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.UserId, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_UserId_CreatedAt");
    }

    /// <summary>
    /// Удалить базу данных (для тестирования/очистки)
    /// ⚠️ ОПАСНО: Удаляет всю базу данных!
    /// </summary>
    public async Task EnsureDeletedAsync()
    {
        await Database.EnsureDeletedAsync();
        _logger?.LogWarning("Database deleted!");
    }

    /// <summary>
    /// Создать базу данных и применить миграции
    /// </summary>
    public async Task EnsureCreatedAsync()
    {
        await Database.EnsureCreatedAsync();
        _logger?.LogInformation("Database created/verified");
    }

    /// <summary>
    /// Применить миграции
    /// </summary>
    public async Task MigrateAsync()
    {
        if ((await Database.GetPendingMigrationsAsync()).Any())
        {
            await Database.MigrateAsync();
            _logger?.LogInformation("Database migrations applied");
        }
        else
        {
            _logger?.LogDebug("No pending migrations");
        }
    }
}

