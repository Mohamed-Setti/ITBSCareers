using System.Collections.Concurrent;

namespace ITBSCareers.Services.Messaging;

public sealed class MessagingPresenceTracker
{
    private readonly ConcurrentDictionary<int, int> _connectionCounts = new();

    public bool Connect(int userId)
    {
        var count = _connectionCounts.AddOrUpdate(userId, 1, (_, current) => current + 1);
        return count == 1;
    }

    public bool Disconnect(int userId)
    {
        while (_connectionCounts.TryGetValue(userId, out var current))
        {
            if (current <= 1)
            {
                return _connectionCounts.TryRemove(userId, out _);
            }

            if (_connectionCounts.TryUpdate(userId, current - 1, current))
            {
                return false;
            }
        }

        return false;
    }

    public bool IsOnline(int userId) => _connectionCounts.ContainsKey(userId);
}
