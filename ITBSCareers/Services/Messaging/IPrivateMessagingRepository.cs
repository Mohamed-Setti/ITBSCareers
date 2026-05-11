using ITBSCareers.Models.Messaging;
using ITBSCareers.Models.Carriere;

namespace ITBSCareers.Services.Messaging;

public interface IPrivateMessagingRepository
{
    Task<User?> GetUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<ConversationListItemViewModel>> GetConversationListAsync(int userId, string? search, CancellationToken cancellationToken = default);
    Task<List<MessagingContactViewModel>> GetContactsAsync(int userId, string? search, CancellationToken cancellationToken = default);
    Task<ConversationViewModel?> GetConversationViewAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationEntityAsync(int conversationId, CancellationToken cancellationToken = default);
    Task<Conversation?> FindConversationBetweenUsersAsync(int userId, int otherUserId, CancellationToken cancellationToken = default);
    Task<bool> CanMessageUserAsync(int userId, int otherUserId, CancellationToken cancellationToken = default);
    Task<MentorshipRequest?> GetContactRequestAsync(int studentId, int alumniId, CancellationToken cancellationToken = default);
    Task<MentorshipRequest?> GetContactRequestByIdAsync(int requestId, int alumniId, CancellationToken cancellationToken = default);
    Task<MentorshipRequest> CreateContactRequestAsync(int studentId, int alumniId, CancellationToken cancellationToken = default);
    Task<bool> SetContactRequestStatusAsync(int requestId, int alumniId, string status, CancellationToken cancellationToken = default);
    Task<bool> HasBlockRelationshipAsync(int userId, int otherUserId, CancellationToken cancellationToken = default);
    Task<bool> UnblockUserAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default);
    Task<Conversation> CreateConversationAsync(int creatorUserId, int otherUserId, string? subject, CancellationToken cancellationToken = default);
    Task<Message> AddMessageAsync(int conversationId, int senderId, int receiverId, string? content, string? attachmentPath, string? attachmentName, string? attachmentContentType, long? attachmentSize, CancellationToken cancellationToken = default);
    Task<bool> MarkConversationReadAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
    Task<bool> ArchiveConversationAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
    Task<bool> BlockUserAsync(int blockerUserId, int blockedUserId, string? reason, CancellationToken cancellationToken = default);
    Task<bool> ReportUserAsync(int reporterUserId, int reportedUserId, string reason, CancellationToken cancellationToken = default);
    Task<bool> DeleteConversationForUserAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
}
