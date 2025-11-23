using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YessGoFront.Data.Entities;

namespace YessGoFront.Services.Domain;

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetNotificationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetNotificationsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates sample notifications for a user (for testing/demo purposes)
    /// </summary>
    /// <param name="userId">ID of the user to create notifications for</param>
    /// <param name="count">Number of sample notifications to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreateSampleNotificationsAsync(int userId, int count = 5, CancellationToken cancellationToken = default);
}
