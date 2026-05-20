using ITBSCareers.Models.Carriere;

namespace ITBSCareers.Services.Notifications;

public interface INotificationService
{
    Task<Notification> CreateAsync(int userId, string type, string content, bool isRead = false, string? subject = null, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetRecentAsync(int userId, int count = 5, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    Task BroadcastUnreadCountAsync(int userId, int unreadCount, CancellationToken cancellationToken = default);
}
