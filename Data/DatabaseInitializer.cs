using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using YessGoFront.Data.Entities;

namespace YessGoFront.Data;

/// <summary>
/// Сервис для инициализации базы данных
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializer>? _logger;
    private const int DatabaseInitTimeoutSeconds = 30;

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
            
            // Для SQLite в мобильном приложении используем EnsureCreatedAsync
            // Это создаст таблицы если их нет, или ничего не сделает если они уже есть
            using var initCts = new CancellationTokenSource(TimeSpan.FromSeconds(DatabaseInitTimeoutSeconds));
            await _context.Database.EnsureCreatedAsync(initCts.Token);
            
            // Настраиваем PRAGMA команды для оптимизации производительности
            await _context.ConfigureSqlitePragmasAsync(initCts.Token);
            
            _logger?.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing database");
            // Повторная попытка
            try
            {
                _logger?.LogWarning("Retrying database initialization...");
                using var retryCts = new CancellationTokenSource(TimeSpan.FromSeconds(DatabaseInitTimeoutSeconds));
                await _context.Database.EnsureCreatedAsync(retryCts.Token);
                await _context.ConfigureSqlitePragmasAsync(retryCts.Token);
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
            
            // Используем общий таймаут для всего seeding (60 секунд)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            
            // 1. Заполняем города
            await SeedCitiesAsync(cts.Token);
            
            // 2. Заполняем тестового пользователя
            await SeedTestUserAsync(cts.Token);
            
            // 3. Заполняем уведомления
            await SeedNotificationsAsync(cts.Token);
            
            // 4. Заполняем транзакции
            await SeedTransactionsAsync(cts.Token);
            
            _logger?.LogInformation("Database seeded successfully");
        }
        catch (OperationCanceledException)
        {
            _logger?.LogError("Database seeding timed out after 60 seconds");
            throw new TimeoutException("Database seeding timed out after 60 seconds");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private async Task SeedCitiesAsync(CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        
        if (await _context.Cities.AnyAsync(cts.Token))
        {
            _logger?.LogDebug("Cities already exist, skipping seed");
            return;
        }

        var cities = new List<City>
        {
            new City
            {
                Name = "Бишкек",
                Code = "BISH",
                Latitude = 42.8746m,
                Longitude = 74.5698m,
                CreatedAt = DateTime.UtcNow
            },
            new City
            {
                Name = "Ош",
                Code = "OSH",
                Latitude = 40.5150m,
                Longitude = 72.8083m,
                CreatedAt = DateTime.UtcNow
            },
            new City
            {
                Name = "Джалал-Абад",
                Code = "JAL",
                Latitude = 40.9333m,
                Longitude = 73.0000m,
                CreatedAt = DateTime.UtcNow
            },
            new City
            {
                Name = "Каракол",
                Code = "KAR",
                Latitude = 42.4907m,
                Longitude = 78.3936m,
                CreatedAt = DateTime.UtcNow
            },
            new City
            {
                Name = "Токмок",
                Code = "TOK",
                Latitude = 42.8292m,
                Longitude = 75.2911m,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Cities.AddRangeAsync(cities, cts.Token);
        await _context.SaveChangesAsync(cts.Token);
        _logger?.LogInformation("Seeded {Count} cities", cities.Count);
    }

    private async Task SeedTestUserAsync(CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(15));
        
        // Проверяем, есть ли уже пользователь с тестовым телефоном
        var testPhone = "+996504876087";
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == testPhone || u.Phone == "996504876087" || u.Phone == "0504876087", cts.Token);

        if (existingUser != null)
        {
            _logger?.LogDebug("Test user already exists, skipping seed");
            
            // Проверяем наличие приветственного уведомления
            var hasWelcomeNotification = await _context.Notifications
                .AnyAsync(n => n.UserId == existingUser.Id && n.Title == "Добро пожаловать в YESS!GO", cts.Token);
            
            if (!hasWelcomeNotification)
            {
                var existingUserWelcomeNotification = new Notification
                {
                    UserId = existingUser.Id,
                    Title = "Добро пожаловать в YESS!GO",
                    Message = "Спасибо за регистрацию в приложении YESS!GO. Желаем приятного пользования!",
                    NotificationType = NotificationType.InApp,
                    Priority = NotificationPriority.Normal,
                    Status = NotificationStatus.Delivered,
                    CreatedAt = DateTime.UtcNow,
                    DeliveredAt = DateTime.UtcNow
                };
                
                await _context.Notifications.AddAsync(existingUserWelcomeNotification, cts.Token);
                await _context.SaveChangesAsync(cts.Token);
                _logger?.LogInformation("Created welcome notification for existing user");
            }
            
            return;
        }

        // Получаем Бишкек для тестового пользователя
        var bishkek = await _context.Cities.FirstOrDefaultAsync(c => c.Name == "Бишкек", cts.Token);
        
        var testUser = new User
        {
            Name = "Тестовый Пользователь",
            Phone = testPhone,
            Email = "testuser@yessgo.kg",
            PhoneVerified = true,
            EmailVerified = false,
            IsActive = true,
            IsBlocked = false,
            PushEnabled = true,
            SmsEnabled = true,
            CityId = bishkek?.Id,
            ReferralCode = "TEST001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(testUser, cts.Token);
        await _context.SaveChangesAsync(cts.Token);
        
        _logger?.LogInformation("Created test user with ID: {UserId}", testUser.Id);

        // Создаём кошелёк для пользователя
        var wallet = new Wallet
        {
            UserId = testUser.Id,
            Balance = 500.00m,
            LastUpdated = DateTime.UtcNow
        };

        await _context.Wallets.AddAsync(wallet, cts.Token);
        await _context.SaveChangesAsync(cts.Token);
        
        _logger?.LogInformation("Created wallet for test user with balance: {Balance}", wallet.Balance);

        // Создаём приветственное уведомление
        var welcomeNotification = new Notification
        {
            UserId = testUser.Id,
            Title = "Добро пожаловать в YESS!GO",
            Message = "Спасибо за регистрацию в приложении YESS!GO. Желаем приятного пользования!",
            NotificationType = NotificationType.InApp,
            Priority = NotificationPriority.Normal,
            Status = NotificationStatus.Delivered,
            CreatedAt = DateTime.UtcNow,
            DeliveredAt = DateTime.UtcNow
        };

        await _context.Notifications.AddAsync(welcomeNotification, cts.Token);
        await _context.SaveChangesAsync(cts.Token);
        
        _logger?.LogInformation("Created welcome notification for test user");
    }

    private async Task SeedNotificationsAsync(CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        
        // Получаем всех пользователей
        var users = await _context.Users.ToListAsync(cts.Token);
        if (!users.Any())
        {
            _logger?.LogDebug("No users found, skipping notifications seed");
            return;
        }

        var random = new Random();
        var now = DateTime.UtcNow;
        var notifications = new List<Notification>();

        // Список тестовых уведомлений
        var sampleNotifications = new List<(string Title, string Message, NotificationType Type, NotificationPriority Priority, double HoursAgo)>
        {
            ("🎉 Добро пожаловать в YessGo!", 
                "Спасибо за регистрацию! Используйте приложение для получения бонусов и кешбэка у наших партнёров.",
                NotificationType.InApp, NotificationPriority.Normal, 48),
            ("💰 Начислен кешбэк", 
                "Вам начислен кешбэк 50 сом за покупку в партнёре «Нават». Проверьте баланс в кошельке!",
                NotificationType.InApp, NotificationPriority.High, 24),
            ("🎁 Специальное предложение", 
                "Скидка 15% на все товары в партнёре «CoffeeTime» до конца недели! Не упустите возможность.",
                NotificationType.Push, NotificationPriority.Normal, 12),
            ("⭐ Новый партнёр", 
                "К нам присоединился новый партнёр «Flask»! Получайте кешбэк 10% на все покупки.",
                NotificationType.InApp, NotificationPriority.Normal, 6),
            ("📱 Обновление приложения", 
                "Доступна новая версия приложения с улучшенным интерфейсом и новыми функциями.",
                NotificationType.InApp, NotificationPriority.Low, 3),
            ("🎯 Бонус за приглашение", 
                "Ваш друг зарегистрировался по вашей реферальной ссылке! Вам начислено 100 YessCoin.",
                NotificationType.InApp, NotificationPriority.High, 1),
            ("🏆 Достижение разблокировано", 
                "Поздравляем! Вы достигли уровня «Бронзовый партнёр». Теперь доступны дополнительные бонусы.",
                NotificationType.InApp, NotificationPriority.Normal, 0.5),
            ("⏰ Напоминание", 
                "Не забудьте использовать промокод BONUS2024 до конца месяца и получить двойной кешбэк!",
                NotificationType.Push, NotificationPriority.Normal, 0.25),
            ("💳 Пополнение баланса", 
                "Ваш баланс пополнен на 500 сом. Спасибо за использование YessGo!",
                NotificationType.InApp, NotificationPriority.High, 0.1),
            ("🎪 Акция выходного дня", 
                "В эти выходные кешбэк увеличен до 20% у всех партнёров категории «Рестораны»!",
                NotificationType.Push, NotificationPriority.High, 0.05),
            ("📊 Еженедельный отчёт", 
                "На этой неделе вы получили 250 сом кешбэка и потратили 1500 сом. Продолжайте в том же духе!",
                NotificationType.InApp, NotificationPriority.Low, 72),
            ("🔔 Новые акции", 
                "У партнёра «Sierra» стартовала акция: каждый 5-й кофе бесплатно!",
                NotificationType.Push, NotificationPriority.Normal, 36),
            ("🎫 Промокод активирован", 
                "Промокод SUMMER2024 успешно применён. Вы получили скидку 10% на следующую покупку.",
                NotificationType.InApp, NotificationPriority.Normal, 18),
            ("📍 Партнёр рядом", 
                "Вы находитесь рядом с партнёром «Bublik»! Зайдите и получите кешбэк 8%.",
                NotificationType.Push, NotificationPriority.Normal, 4),
            ("🎁 День рождения", 
                "С днём рождения! В честь вашего праздника дарим 200 YessCoin. Используйте их для получения бонусов.",
                NotificationType.InApp, NotificationPriority.Urgent, 0.01)
        };

        // Создаём уведомления для каждого пользователя
        foreach (var user in users)
        {
            // Проверяем, есть ли уже уведомления у пользователя
            var existingCount = await _context.Notifications
                .CountAsync(n => n.UserId == user.Id);
            
            if (existingCount >= 10)
            {
                _logger?.LogDebug("User {UserId} already has {Count} notifications, skipping", user.Id, existingCount);
                continue;
            }

            // Создаём уведомления для пользователя (кроме приветственного, оно уже создаётся в SeedTestUserAsync)
            foreach (var sample in sampleNotifications)
            {
                // Пропускаем приветственное уведомление, оно уже создаётся отдельно
                if (sample.Title.Contains("Добро пожаловать"))
                    continue;
                
                var createdAt = now.AddHours(-sample.HoursAgo);
                var isRead = random.Next(3) == 0; // 33% прочитанных
                
                var notification = new Notification
                {
                    UserId = user.Id,
                    Title = sample.Title,
                    Message = sample.Message,
                    NotificationType = sample.Type,
                    Priority = sample.Priority,
                    Status = NotificationStatus.Delivered,
                    CreatedAt = createdAt,
                    DeliveredAt = createdAt.AddMinutes(1),
                    ReadAt = isRead ? createdAt.AddMinutes(random.Next(5, 60)) : null
                };
                
                notifications.Add(notification);
            }
        }

        if (notifications.Any())
        {
            await _context.Notifications.AddRangeAsync(notifications, cts.Token);
            await _context.SaveChangesAsync(cts.Token);
            _logger?.LogInformation("Seeded {Count} notifications for {UserCount} users", notifications.Count, users.Count);
        }
        else
        {
            _logger?.LogDebug("No new notifications to seed");
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
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await _context.Database.EnsureDeletedAsync(cts.Token);
            await _context.Database.EnsureCreatedAsync(cts.Token);
            
            _logger?.LogInformation("Database reset successfully");
        }
        catch (OperationCanceledException)
        {
            _logger?.LogError("Database reset timed out after 30 seconds");
            throw new TimeoutException("Database reset timed out after 30 seconds");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error resetting database");
            throw;
        }
    }

    private async Task SeedTransactionsAsync(CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        
        // Получаем всех пользователей
        var users = await _context.Users.ToListAsync(cts.Token);
        if (!users.Any())
        {
            _logger?.LogDebug("No users found, skipping transactions seed");
            return;
        }

        // Получаем партнёров для транзакций
        var partners = await _context.Partners.ToListAsync(cts.Token);
        
        // Получаем кошельки пользователей
        var wallets = await _context.Wallets.ToListAsync(cts.Token);
        var walletDict = wallets.ToDictionary(w => w.UserId);

        var random = new Random();
        var now = DateTime.UtcNow;
        var transactions = new List<Transaction>();

        // Типы транзакций
        var transactionTypes = new[] { "topup", "discount", "bonus", "refund", "payment" };
        var statuses = new[] { "completed", "pending", "failed" };

        // Создаём транзакции для каждого пользователя
        foreach (var user in users)
        {
            // Проверяем, есть ли уже транзакции у пользователя
            var existingCount = await _context.Transactions
                .CountAsync(t => t.UserId == user.Id);
            
            if (existingCount >= 20)
            {
                _logger?.LogDebug("User {UserId} already has {Count} transactions, skipping", user.Id, existingCount);
                continue;
            }

            var wallet = walletDict.GetValueOrDefault(user.Id);
            var currentBalance = wallet?.Balance ?? 0m;

            // Создаём 15-20 транзакций для каждого пользователя
            var transactionCount = random.Next(15, 21);
            
            for (int i = 0; i < transactionCount; i++)
            {
                var type = transactionTypes[random.Next(transactionTypes.Length)];
                var status = statuses[random.Next(statuses.Length)];
                var createdAt = now.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 24));
                
                decimal amount;
                decimal? balanceBefore = null;
                decimal? balanceAfter = null;
                int? partnerId = null;

                // Определяем сумму и партнёра в зависимости от типа
                switch (type)
                {
                    case "topup":
                        amount = new[] { 100m, 200m, 300m, 500m, 1000m }[random.Next(5)];
                        balanceBefore = currentBalance;
                        currentBalance += amount;
                        balanceAfter = currentBalance;
                        break;
                    case "bonus":
                        amount = new[] { 10m, 20m, 50m, 100m, 200m }[random.Next(5)];
                        balanceBefore = currentBalance;
                        currentBalance += amount;
                        balanceAfter = currentBalance;
                        break;
                    case "refund":
                        amount = new[] { 50m, 100m, 150m, 200m }[random.Next(4)];
                        balanceBefore = currentBalance;
                        currentBalance += amount;
                        balanceAfter = currentBalance;
                        break;
                    case "discount":
                    case "payment":
                        if (partners.Any())
                        {
                            var partner = partners[random.Next(partners.Count)];
                            partnerId = partner.Id;
                            amount = new[] { 100m, 200m, 300m, 500m, 800m, 1000m }[random.Next(6)];
                            balanceBefore = currentBalance;
                            currentBalance -= amount;
                            balanceAfter = currentBalance;
                        }
                        else
                        {
                            continue; // Пропускаем, если нет партнёров
                        }
                        break;
                    default:
                        amount = 100m;
                        break;
                }

                var transaction = new Transaction
                {
                    UserId = user.Id,
                    Type = type,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    Status = status,
                    PartnerId = partnerId,
                    CreatedAt = createdAt,
                    CompletedAt = status == "completed" ? createdAt.AddMinutes(random.Next(1, 60)) : null
                };

                transactions.Add(transaction);
            }

            // Обновляем баланс кошелька
            if (wallet != null)
            {
                wallet.Balance = currentBalance;
                wallet.LastUpdated = DateTime.UtcNow;
            }
        }

        if (transactions.Any())
        {
            await _context.Transactions.AddRangeAsync(transactions, cts.Token);
            await _context.SaveChangesAsync(cts.Token);
            _logger?.LogInformation("Seeded {Count} transactions for {UserCount} users", transactions.Count, users.Count);
        }
        else
        {
            _logger?.LogDebug("No new transactions to seed");
        }
    }
}

