using ITBSCareers.Hubs;
using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly CarriereDbContext _context;
    private readonly IHubContext<MessagingHub> _hubContext;

    public NotificationService(CarriereDbContext context, IHubContext<MessagingHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<Notification> CreateAsync(int userId, string type, string content, bool isRead = false, string? subject = null, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Content = content,
            IsRead = isRead,
            CreatedAt = DateTime.Now
        };

        _context.Notifications.Add(notification);

        var userEmail = await _context.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        if (await EmailLogsTableExistsAsync(cancellationToken))
        {
            _context.EmailLogs.Add(new EmailLog
            {
                UserId = userId,
                ToEmail = userEmail,
                Subject = subject ?? $"ITBS Careers - {type}",
                Body = content,
                CreatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        var unreadCount = await GetUnreadCountAsync(userId, cancellationToken);
        await BroadcastUnreadCountAsync(userId, unreadCount, cancellationToken);
        await _hubContext.Clients.Group(MessagingHub.GetUserGroupName(userId)).SendAsync("NotificationReceived", new
        {
            notificationId = notification.NotificationId,
            userId,
            type,
            content,
            createdAt = notification.CreatedAt,
            unreadCount
        }, cancellationToken);

        return notification;
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
        => _context.Notifications.CountAsync(n => n.UserId == userId && n.IsRead != true, cancellationToken);

    public async Task<List<Notification>> GetRecentAsync(int userId, int count = 5, CancellationToken cancellationToken = default)
        => await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(count, 1, 10))
            .ToListAsync(cancellationToken);

    public async Task<int> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var unreadItems = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead != true)
            .ToListAsync(cancellationToken);

        foreach (var item in unreadItems)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.Group(MessagingHub.GetUserGroupName(userId)).SendAsync("NotificationBadgeUpdated", new
        {
            userId,
            unreadCount = 0
        }, cancellationToken);

        return unreadItems.Count;
    }

    public Task BroadcastUnreadCountAsync(int userId, int unreadCount, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(MessagingHub.GetUserGroupName(userId)).SendAsync("NotificationBadgeUpdated", new
        {
            userId,
            unreadCount
        }, cancellationToken);

    private async Task<bool> EmailLogsTableExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CASE WHEN OBJECT_ID('dbo.EmailLogs', 'U') IS NOT NULL THEN 1 ELSE 0 END";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is int value && value == 1;
        }
        catch
        {
            return false;
        }
    }
}
