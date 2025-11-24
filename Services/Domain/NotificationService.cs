using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YessGoFront.Data;
using YessGoFront.Data.Entities;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Services.Api;

namespace YessGoFront.Services.Domain;

/// <summary>
/// Реализация сервиса уведомлений
/// Получает данные из API и кэширует в локальной БД
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService>? _logger;
    private readonly AppDbContext _dbContext;
    private readonly INotificationApiService _apiService;

    public NotificationService(
        AppDbContext dbContext,
        INotificationApiService apiService,
        ILogger<NotificationService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _logger = logger;
    }

    public async Task<IEnumerable<Notification>> GetNotificationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await GetNotificationsAsync(userId, 1, 100, cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Getting notifications for user {UserId}, page: {Page}, pageSize: {PageSize}", 
                userId, page, pageSize);
            
            // Сначала пытаемся получить данные из API (PostgreSQL)
            try
            {
                var apiNotifications = await _apiService.GetNotificationsAsync(page, pageSize, cancellationToken);
                
                if (apiNotifications.Any())
                {
                    _logger?.LogInformation("Received {Count} notifications from API, syncing to local DB", apiNotifications.Count());
                    
                    // Синхронизируем с локальной БД
                    await SyncNotificationsToLocalDbAsync(userId, apiNotifications, cancellationToken);
                    
                    // Возвращаем из локальной БД (после синхронизации)
                    return await _dbContext.Notifications
                        .Where(n => n.UserId == userId)
                        .OrderByDescending(n => n.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync(cancellationToken);
                }
            }
            catch (Exception apiEx)
            {
                _logger?.LogWarning(apiEx, "Failed to fetch notifications from API, falling back to local DB");
            }
            
            // Если API недоступен или вернул пустой результат, используем локальную БД
            _logger?.LogDebug("Using local DB for notifications");
            return await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting notifications for user {UserId}, page: {Page}", userId, page);
            throw new NetworkException("Не удалось загрузить уведомления", ex);
        }
    }
    
    private async Task SyncNotificationsToLocalDbAsync(
        int userId, 
        IEnumerable<Notification> apiNotifications, 
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var apiNotification in apiNotifications)
            {
                // Проверяем, существует ли уже это уведомление в локальной БД
                var existing = await _dbContext.Notifications
                    .FirstOrDefaultAsync(n => n.Id == apiNotification.Id && n.UserId == userId, cancellationToken);
                
                if (existing == null)
                {
                    // Создаём новое уведомление
                    apiNotification.UserId = userId; // Убеждаемся, что UserId правильный
                    await _dbContext.Notifications.AddAsync(apiNotification, cancellationToken);
                }
                else
                {
                    // Обновляем существующее уведомление
                    existing.Title = apiNotification.Title;
                    existing.Message = apiNotification.Message;
                    existing.NotificationType = apiNotification.NotificationType;
                    existing.Priority = apiNotification.Priority;
                    existing.Status = apiNotification.Status;
                    existing.ReadAt = apiNotification.ReadAt;
                    existing.DeliveredAt = apiNotification.DeliveredAt;
                    existing.Data = apiNotification.Data;
                }
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger?.LogDebug("Synced {Count} notifications to local DB", apiNotifications.Count());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing notifications to local DB");
            // Не пробрасываем исключение, чтобы не прервать основной поток
        }
    }

    public async Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Marking notification {NotificationId} as read", notificationId);
            
            // Сначала обновляем в API
            try
            {
                await _apiService.MarkAsReadAsync(notificationId, cancellationToken);
            }
            catch (Exception apiEx)
            {
                _logger?.LogWarning(apiEx, "Failed to mark notification as read in API, updating local DB only");
            }
            
            // Затем обновляем в локальной БД
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
            if (notification != null)
            {
                notification.ReadAt = DateTime.UtcNow;
                notification.Status = NotificationStatus.Read;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            throw new NetworkException("Не удалось отметить уведомление как прочитанное", ex);
        }
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Marking all notifications as read for user {UserId}", userId);
            
            // Сначала обновляем в API
            try
            {
                await _apiService.MarkAllAsReadAsync(userId, cancellationToken);
            }
            catch (Exception apiEx)
            {
                _logger?.LogWarning(apiEx, "Failed to mark all notifications as read in API, updating local DB only");
            }
            
            // Затем обновляем в локальной БД
            var unreadNotifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .ToListAsync(cancellationToken);

            foreach (var notification in unreadNotifications)
            {
                notification.ReadAt = DateTime.UtcNow;
                notification.Status = NotificationStatus.Read;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw new NetworkException("Не удалось отметить все уведомления как прочитанные", ex);
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Getting unread notifications count for user {UserId}", userId);
            
            // Сначала пытаемся получить из API
            try
            {
                var apiCount = await _apiService.GetUnreadCountAsync(userId, cancellationToken);
                _logger?.LogDebug("Unread count from API: {Count}", apiCount);
                return apiCount;
            }
            catch (Exception apiEx)
            {
                _logger?.LogWarning(apiEx, "Failed to get unread count from API, using local DB");
            }
            
            // Если API недоступен, используем локальную БД
            return await _dbContext.Notifications
                .CountAsync(n => n.UserId == userId && n.ReadAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting unread notifications count for user {UserId}", userId);
            throw new NetworkException("Не удалось получить количество непрочитанных уведомлений", ex);
        }
    }

    /// <summary>
    /// Creates sample notifications for a user (for testing/demo purposes)
    /// </summary>
    public async Task CreateSampleNotificationsAsync(int userId, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Creating {Count} sample notifications for user {UserId}", count, userId);
            
            var notifications = new List<Notification>();
            var now = DateTime.UtcNow;
            var random = new Random();
            
            // Список реалистичных уведомлений для приложения YessGo (в стиле из дизайна)
            var sampleNotifications = new List<(string Title, string Message, NotificationType Type, NotificationPriority Priority, double HoursAgo)>
            {
                ("🏆 Бонусы", 
                    "Оплачивайте через QR Yess!Go и экономьте деньги. Bishkek Petroleum, Планета электроники, Бимед Фарм, Азия и еще 100+ партнеров! Перейдите в раздел \"Бонусы\" в Yess!Go и ознакомьтесь с ними подробнее!",
                    NotificationType.InApp, NotificationPriority.Normal, 48),
                ("💰 Начислен кешбэк", 
                    "Начислено: 0,14 Б за покупку в Азия. Доступно: 0,14 Б",
                    NotificationType.InApp, NotificationPriority.High, 24),
                ("🏆 Достижение", 
                    "Ваш новый уровень на Октябрь: Бронза",
                    NotificationType.InApp, NotificationPriority.Normal, 12),
                ("💰 Кешбэк начислен", 
                    "Вам начислен кешбэк 50 сом за покупку в партнёре «Нават». Проверьте баланс в кошельке!",
                    NotificationType.InApp, NotificationPriority.High, 6),
                ("🎁 Специальное предложение", 
                    "Скидка 15% на все товары в партнёре «CoffeeTime» до конца недели! Не упустите возможность сэкономить.",
                    NotificationType.Push, NotificationPriority.Normal, 3),
                ("🎯 Бонус за приглашение", 
                    "Ваш друг зарегистрировался по вашей реферальной ссылке! Вам начислено 100 YessCoin. Продолжайте приглашать друзей и получайте бонусы!",
                    NotificationType.InApp, NotificationPriority.High, 1),
                ("🏆 Достижение разблокировано", 
                    "Поздравляем! Вы достигли уровня «Бронзовый партнёр». Теперь доступны дополнительные бонусы и привилегии.",
                    NotificationType.InApp, NotificationPriority.Normal, 0.5),
                ("⏰ Напоминание", 
                    "Не забудьте использовать промокод BONUS2024 до конца месяца и получить двойной кешбэк на все покупки!",
                    NotificationType.Push, NotificationPriority.Normal, 0.25),
                ("💰 Пополнение баланса", 
                    "Ваш баланс пополнен на 500 сом. Спасибо за использование YessGo! Теперь вы можете использовать средства для оплаты у партнёров.",
                    NotificationType.InApp, NotificationPriority.High, 0.1),
                ("🎪 Акция выходного дня", 
                    "В эти выходные кешбэк увеличен до 20% у всех партнёров категории «Рестораны»! Не упустите возможность получить больше бонусов.",
                    NotificationType.Push, NotificationPriority.High, 0.05),
                ("📊 Еженедельный отчёт", 
                    "На этой неделе вы получили 250 сом кешбэка и потратили 1500 сом. Продолжайте в том же духе и получайте еще больше бонусов!",
                    NotificationType.InApp, NotificationPriority.Low, 72),
                ("🔔 Новые акции", 
                    "У партнёра «Sierra» стартовала акция: каждый 5-й кофе бесплатно! Заходите и пользуйтесь выгодным предложением.",
                    NotificationType.Push, NotificationPriority.Normal, 36),
                ("🎫 Промокод активирован", 
                    "Промокод SUMMER2024 успешно применён. Вы получили скидку 10% на следующую покупку. Используйте её в течение 30 дней.",
                    NotificationType.InApp, NotificationPriority.Normal, 18),
                ("📍 Партнёр рядом", 
                    "Вы находитесь рядом с партнёром «Bublik»! Зайдите и получите кешбэк 8% на все покупки. Не упустите возможность сэкономить!",
                    NotificationType.Push, NotificationPriority.Normal, 4),
                ("🎁 День рождения", 
                    "С днём рождения! В честь вашего праздника дарим 200 YessCoin. Используйте их для получения бонусов и скидок у наших партнёров.",
                    NotificationType.InApp, NotificationPriority.Urgent, 0.01)
            };
            
            // Создаём уведомления
            for (int i = 0; i < Math.Min(count, sampleNotifications.Count); i++)
            {
                var sample = sampleNotifications[i];
                var createdAt = now.AddHours(-sample.HoursAgo);
                var isRead = random.Next(3) == 0; // 33% прочитанных
                
                var notification = new Notification
                {
                    UserId = userId,
                    Title = sample.Title,
                    Message = sample.Message,
                    NotificationType = sample.Type,
                    Priority = sample.Priority,
                    Status = NotificationStatus.Delivered,
                    CreatedAt = createdAt,
                    DeliveredAt = createdAt.AddMinutes(1),
                    ReadAt = isRead ? createdAt.AddMinutes(random.Next(5, 60)) : null,
                    Data = new Dictionary<string, object>
                    {
                        ["type"] = "sample",
                        ["sampleId"] = i + 1,
                        ["category"] = GetCategoryFromTitle(sample.Title)
                    }
                };
                
                notifications.Add(notification);
            }
            
            // Если нужно больше уведомлений, чем есть в списке, добавляем случайные
            if (count > sampleNotifications.Count)
            {
                for (int i = sampleNotifications.Count; i < count; i++)
                {
                    var randomSample = sampleNotifications[random.Next(sampleNotifications.Count)];
                    var createdAt = now.AddHours(-random.Next(1, 168)); // Последние 7 дней
                    var isRead = random.Next(3) == 0;
                    
                    var notification = new Notification
                    {
                        UserId = userId,
                        Title = randomSample.Title,
                        Message = randomSample.Message,
                        NotificationType = randomSample.Type,
                        Priority = randomSample.Priority,
                        Status = NotificationStatus.Delivered,
                        CreatedAt = createdAt,
                        DeliveredAt = createdAt.AddMinutes(1),
                        ReadAt = isRead ? createdAt.AddMinutes(random.Next(5, 60)) : null,
                        Data = new Dictionary<string, object>
                        {
                            ["type"] = "sample",
                            ["sampleId"] = i + 1
                        }
                    };
                    
                    notifications.Add(notification);
                }
            }
            
            await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger?.LogInformation("Successfully created {Count} sample notifications for user {UserId}", notifications.Count, userId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating sample notifications for user {UserId}", userId);
            throw new NetworkException("Не удалось создать тестовые уведомления", ex);
        }
    }
    
    private string GetCategoryFromTitle(string title)
    {
        var titleLower = title.ToLower();
        
        if (titleLower.Contains("кешбэк") || titleLower.Contains("баланс") || titleLower.Contains("пополнен") || titleLower.Contains("начислен"))
            return "finance";
        if (titleLower.Contains("партнёр") || titleLower.Contains("акци") || titleLower.Contains("предложен") || titleLower.Contains("скидк") || titleLower.Contains("промокод"))
            return "promotion";
        if (titleLower.Contains("приглашен") || titleLower.Contains("реферал") || titleLower.Contains("друг") || titleLower.Contains("бонус за"))
            return "referral";
        if (titleLower.Contains("достижен") || titleLower.Contains("уровен") || titleLower.Contains("бронз") || titleLower.Contains("серебр") || titleLower.Contains("золот"))
            return "achievement";
        if (titleLower.Contains("отчёт") || titleLower.Contains("статистик"))
            return "general";
        return "general";
    }

    public async Task DeleteSampleNotificationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Deleting sample notifications for user {UserId}, keeping welcome notification", userId);
            
            // Удаляем все уведомления, кроме приветственного
            var sampleNotifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && n.Title != "Добро пожаловать в YESS!GO")
                .ToListAsync(cancellationToken);
            
            if (sampleNotifications.Any())
            {
                _dbContext.Notifications.RemoveRange(sampleNotifications);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger?.LogInformation("Deleted {Count} sample notifications for user {UserId}", sampleNotifications.Count, userId);
            }
            else
            {
                _logger?.LogDebug("No sample notifications found for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting sample notifications for user {UserId}", userId);
            throw new NetworkException("Не удалось удалить тестовые уведомления", ex);
        }
    }
}
