using ITBSCareers.Models.Messaging;

namespace ITBSCareers.Services.Messaging;

public interface IPrivateMessagingService
{
    Task<MessagingIndexViewModel> GetInboxAsync(int userId, string? search, string? contactsSearch, int? conversationId = null, CancellationToken cancellationToken = default);
    Task<ConversationViewModel?> GetConversationAsync(int userId, int conversationId, CancellationToken cancellationToken = default);
    Task<(bool success, int conversationId, string? error)> StartConversationAsync(int userId, int otherUserId, string? subject, CancellationToken cancellationToken = default);
    Task<(bool success, string? error, bool pending)> RequestContactAsync(int studentUserId, int alumniUserId, CancellationToken cancellationToken = default);
    Task<(bool success, string? error, int? conversationId)> RespondToContactRequestAsync(int alumniUserId, int requestId, bool accepted, CancellationToken cancellationToken = default);
    Task<(bool success, MessageViewModel? message, string? error, int unreadCount)> SendMessageAsync(int userId, int conversationId, string? content, string? attachmentPath, string? attachmentName, string? attachmentContentType, long? attachmentSize, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(int userId, int conversationId, CancellationToken cancellationToken = default);
    Task<bool> BlockUserAsync(int blockerUserId, int blockedUserId, string? reason, CancellationToken cancellationToken = default);
    Task<bool> UnblockUserAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default);
    Task<bool> ReportUserAsync(int reporterUserId, int reportedUserId, string reason, CancellationToken cancellationToken = default);
    Task<bool> DeleteConversationAsync(int userId, int conversationId, CancellationToken cancellationToken = default);
}
