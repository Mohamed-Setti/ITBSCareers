using System.Security.Claims;
using ITBSCareers.Services.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ITBSCareers.Hubs;

[Authorize]
public class MessagingHub : Hub
{
    private readonly MessagingPresenceTracker _presenceTracker;

    public MessagingHub(MessagingPresenceTracker presenceTracker)
    {
        _presenceTracker = presenceTracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId.Value));

            if (_presenceTracker.Connect(userId.Value))
            {
                await Clients.All.SendAsync("PresenceChanged", new
                {
                    userId = userId.Value,
                    isOnline = true
                });
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue && _presenceTracker.Disconnect(userId.Value))
        {
            await Clients.All.SendAsync("PresenceChanged", new
            {
                userId = userId.Value,
                isOnline = false
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
    }

    public async Task SetTyping(int conversationId, bool isTyping)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return;
        }

        var userName = GetCurrentUserName();
        await Clients.Group(GetConversationGroupName(conversationId)).SendAsync("TypingStateChanged", new
        {
            conversationId,
            userId = userId.Value,
            userName,
            isTyping
        });
    }

    public static string GetUserGroupName(int userId) => $"user-{userId}";

    public static string GetConversationGroupName(int conversationId) => $"conversation-{conversationId}";

    private int? GetCurrentUserId()
    {
        var claimValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claimValue, out var id))
        {
            return id;
        }

        return null;
    }

    private string GetCurrentUserName()
    {
        return Context.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    }
}
