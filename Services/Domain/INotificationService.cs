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
}
