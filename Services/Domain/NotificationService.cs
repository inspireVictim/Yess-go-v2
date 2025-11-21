using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YessGoFront.Data;
using YessGoFront.Data.Entities;
using YessGoFront.Infrastructure.Exceptions;

namespace YessGoFront.Services.Domain;

/// <summary>
/// Реализация сервиса уведомлений
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService>? _logger;
    private readonly AppDbContext _dbContext;

    public NotificationService(
        AppDbContext dbContext,
        ILogger<NotificationService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<IEnumerable<Notification>> GetNotificationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Getting notifications for user {UserId}", userId);
            
            return await Task.FromResult(_dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting notifications for user {UserId}", userId);
            throw new NetworkException("Не удалось загрузить уведомления", ex);
        }
    }

    public async Task<IEnumerable<Notification>> GetNotificationsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Getting notifications for user {UserId}, page: {Page}, pageSize: {PageSize}", 
                userId, page, pageSize);
            
            return await Task.FromResult(_dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting notifications for user {UserId}, page: {Page}", userId, page);
            throw new NetworkException("Не удалось загрузить уведомления", ex);
        }
    }

    public async Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Marking notification {NotificationId} as read", notificationId);
            
            var notification = _dbContext.Notifications.FirstOrDefault(n => n.Id == notificationId);
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
            
            var unreadNotifications = _dbContext.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .ToList();

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
            
            return await Task.FromResult(_dbContext.Notifications
                .Count(n => n.UserId == userId && n.ReadAt == null));
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
    public async Task CreateSampleNotificationsAsync(int userId, int count = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Creating {Count} sample notifications for user {UserId}", count, userId);
            
            var notifications = new List<Notification>();
            var now = DateTime.UtcNow;
            var random = new Random();
            
            var notificationTypes = Enum.GetValues<NotificationType>();
            var priorities = Enum.GetValues<NotificationPriority>();
            
            for (int i = 0; i < count; i++)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = GetSampleTitle(i),
                    Message = GetSampleMessage(i),
                    NotificationType = notificationTypes[random.Next(notificationTypes.Length)],
                    Priority = priorities[random.Next(priorities.Length)],
                    Status = NotificationStatus.Delivered,
                    CreatedAt = now.AddMinutes(-random.Next(5, 1440)), // Between 5 minutes and 24 hours ago
                    ReadAt = random.Next(3) == 0 ? now.AddMinutes(-random.Next(5)) : (DateTime?)null,
                    Data = new Dictionary<string, object>
                    {
                        ["type"] = "sample",
                        ["sampleId"] = i + 1
                    }
                };
                
                notifications.Add(notification);
            }
            
            await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating sample notifications for user {UserId}", userId);
            throw new NetworkException("Не удалось создать тестовые уведомления", ex);
        }
    }
    
    private string GetSampleTitle(int index)
    {
        var titles = new[]
        {
            "Новое уведомление",
            "Обновление статуса заказа",
            "Специальное предложение",
            "Важная информация",
            "Напоминание"
        };
        
        return titles[index % titles.Length];
    }
    
    private string GetSampleMessage(int index)
    {
        var messages = new[]
        {
            "Ваш заказ #12345 был успешно доставлен. Надеемся, вам все понравилось!",
            "У нас для вас специальное предложение! Скидка 20% на все товары в течение 24 часов.",
            "Не забудьте оставить отзыв о недавнем заказе. Ваше мнение очень важно для нас.",
            "Ваш возврат был обработан. Средства будут зачислены в течение 3-5 рабочих дней.",
            "Спасибо за подписку на нашу рассылку! Вот ваш промокод на скидку: WELCOME10"
        };
        
        return messages[index % messages.Length];
    }
}
