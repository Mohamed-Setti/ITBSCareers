using ITBSCareers.Models.Messaging;
using ITBSCareers.Models.Carriere;
using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Services.Messaging;

public class PrivateMessagingService : IPrivateMessagingService
{
    private readonly IPrivateMessagingRepository _repository;

    public PrivateMessagingService(IPrivateMessagingRepository repository)
    {
        _repository = repository;
    }

    public async Task<MessagingIndexViewModel> GetInboxAsync(int userId, string? search, string? contactsSearch, int? conversationId = null, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetUserAsync(userId, cancellationToken);
        if (user == null)
        {
            return new MessagingIndexViewModel();
        }

        var conversations = await _repository.GetConversationListAsync(userId, search, cancellationToken);
        var eligibleContacts = await _repository.GetContactsAsync(userId, contactsSearch, cancellationToken);
        var unreadCount = await _repository.GetUnreadCountAsync(userId, cancellationToken);

        ConversationViewModel? selectedConversation = null;
        if (conversationId.HasValue)
        {
            selectedConversation = await _repository.GetConversationViewAsync(conversationId.Value, userId, cancellationToken);
        }

        if (selectedConversation == null && conversations.Count > 0)
        {
            selectedConversation = await _repository.GetConversationViewAsync(conversations[0].ConversationId, userId, cancellationToken);
        }

        return new MessagingIndexViewModel
        {
            CurrentUserId = userId,
            CurrentUserName = user.FullName,
            Search = search,
            ContactsSearch = contactsSearch,
            UnreadCount = unreadCount,
            Conversations = conversations,
            EligibleContacts = eligibleContacts,
            SelectedConversation = selectedConversation
        };
    }

    public Task<ConversationViewModel?> GetConversationAsync(int userId, int conversationId, CancellationToken cancellationToken = default)
    {
        return _repository.GetConversationViewAsync(conversationId, userId, cancellationToken);
    }

    public async Task<(bool success, int conversationId, string? error)> StartConversationAsync(int userId, int otherUserId, string? subject, CancellationToken cancellationToken = default)
    {
        if (userId == otherUserId)
        {
            return (false, 0, "La conversation avec soi-même est impossible.");
        }

        var otherUser = await _repository.GetUserAsync(otherUserId, cancellationToken);
        if (otherUser == null)
        {
            return (false, 0, "Utilisateur introuvable.");
        }

        if (await _repository.HasBlockRelationshipAsync(userId, otherUserId, cancellationToken))
        {
            return (false, 0, "Conversation bloquée entre ces utilisateurs.");
        }

        if (!await _repository.CanMessageUserAsync(userId, otherUserId, cancellationToken))
        {
            return (false, 0, "Profil privé. Une demande de contact est requise.");
        }

        var existing = await _repository.FindConversationBetweenUsersAsync(userId, otherUserId, cancellationToken);
        if (existing != null)
        {
            return (true, existing.ConversationId, null);
        }

        var created = await _repository.CreateConversationAsync(userId, otherUserId, subject, cancellationToken);
        return (true, created.ConversationId, null);
    }

    public async Task<(bool success, string? error, bool pending)> RequestContactAsync(int studentUserId, int alumniUserId, CancellationToken cancellationToken = default)
    {
        if (studentUserId == alumniUserId)
        {
            return (false, "La demande de contact avec soi-même est impossible.", false);
        }

        if (await _repository.HasBlockRelationshipAsync(studentUserId, alumniUserId, cancellationToken))
        {
            return (false, "Impossible d'envoyer une demande de contact à cet utilisateur.", false);
        }

        var alumni = await _repository.GetUserAsync(alumniUserId, cancellationToken);
        if (alumni?.Alumni == null)
        {
            return (false, "Alumni introuvable.", false);
        }

        if (alumni.Alumni.IsContactPublic)
        {
            return (false, "Ce profil est public; la demande de contact n'est pas nécessaire.", false);
        }

        if (await _repository.CanMessageUserAsync(studentUserId, alumniUserId, cancellationToken))
        {
            return (true, null, false);
        }

        var existing = await _repository.GetContactRequestAsync(studentUserId, alumniUserId, cancellationToken);
        if (existing != null)
        {
            if (existing.Status == "Pending")
            {
                return (true, null, true);
            }

            if (existing.Status == "Accepted")
            {
                return (true, null, false);
            }
        }

        var request = await _repository.CreateContactRequestAsync(studentUserId, alumniUserId, cancellationToken);
        return (true, null, request.Status == "Pending");
    }

    public async Task<(bool success, string? error, int? conversationId)> RespondToContactRequestAsync(int alumniUserId, int requestId, bool accepted, CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetContactRequestByIdAsync(requestId, alumniUserId, cancellationToken);
        if (request == null)
        {
            return (false, "Demande de contact introuvable.", null);
        }

        if (request.Status != "Pending")
        {
            return (false, "Cette demande a déjà été traitée.", null);
        }

        var student = await _repository.GetUserAsync(request.StudentId, cancellationToken);
        var alumni = await _repository.GetUserAsync(alumniUserId, cancellationToken);
        if (student == null || alumni == null)
        {
            return (false, "Utilisateur introuvable.", null);
        }

        if (!accepted)
        {
            var rejected = await _repository.SetContactRequestStatusAsync(requestId, alumniUserId, "Rejected", cancellationToken);
            if (!rejected)
            {
                return (false, "Impossible de refuser la demande.", null);
            }

            return (true, null, null);
        }

        var updated = await _repository.SetContactRequestStatusAsync(requestId, alumniUserId, "Accepted", cancellationToken);
        if (!updated)
        {
            return (false, "Impossible d'accepter la demande.", null);
        }

        var existingConversation = await _repository.FindConversationBetweenUsersAsync(student.UserId, alumniUserId, cancellationToken);
        if (existingConversation != null)
        {
            return (true, null, existingConversation.ConversationId);
        }

        var conversation = await _repository.CreateConversationAsync(student.UserId, alumniUserId, "Demande de contact acceptée", cancellationToken);
        return (true, null, conversation.ConversationId);
    }

    public async Task<(bool success, MessageViewModel? message, string? error, int unreadCount)> SendMessageAsync(int userId, int conversationId, string? content, string? attachmentPath, string? attachmentName, string? attachmentContentType, long? attachmentSize, CancellationToken cancellationToken = default)
    {
        var sender = await _repository.GetUserAsync(userId, cancellationToken);
        var conversation = await _repository.GetConversationEntityAsync(conversationId, cancellationToken);
        if (conversation == null || conversation.IsDeleted)
        {
            return (false, null, "Conversation introuvable.", 0);
        }

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId && !p.IsArchived);
        if (participant == null)
        {
            return (false, null, "Accès refusé.", 0);
        }

        var receiverId = conversation.Participants.FirstOrDefault(p => p.UserId != userId && !p.IsArchived)?.UserId;
        if (!receiverId.HasValue)
        {
            return (false, null, "Destinataire introuvable.", 0);
        }

        if (await _repository.HasBlockRelationshipAsync(userId, receiverId.Value, cancellationToken))
        {
            return (false, null, "Impossible d'envoyer un message dans cette conversation.", 0);
        }

        var hasText = !string.IsNullOrWhiteSpace(content);
        var hasAttachment = !string.IsNullOrWhiteSpace(attachmentPath);
        if (!hasText && !hasAttachment)
        {
            return (false, null, "Le message ne peut pas être vide.", 0);
        }

        var message = await _repository.AddMessageAsync(
            conversationId,
            userId,
            receiverId.Value,
            content,
            attachmentPath,
            attachmentName,
            attachmentContentType,
            attachmentSize,
            cancellationToken);

        var unreadCount = await _repository.GetUnreadCountAsync(receiverId.Value, cancellationToken);

        return (true, new MessageViewModel
        {
            MessageId = message.MessageId,
            SenderId = message.SenderId,
            SenderName = sender?.FullName ?? string.Empty,
            Content = message.Content,
            AttachmentPath = message.AttachmentPath,
            AttachmentName = message.AttachmentName,
            IsRead = message.IsRead,
            SentAt = message.SentAt,
            IsMine = true
        }, null, unreadCount);
    }

    public Task<bool> MarkReadAsync(int userId, int conversationId, CancellationToken cancellationToken = default)
    {
        return _repository.MarkConversationReadAsync(conversationId, userId, cancellationToken);
    }

    public Task<bool> BlockUserAsync(int blockerUserId, int blockedUserId, string? reason, CancellationToken cancellationToken = default)
    {
        return _repository.BlockUserAsync(blockerUserId, blockedUserId, reason, cancellationToken);
    }

    public Task<bool> UnblockUserAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default)
    {
        return _repository.UnblockUserAsync(blockerUserId, blockedUserId, cancellationToken);
    }

    public Task<bool> ReportUserAsync(int reporterUserId, int reportedUserId, string reason, CancellationToken cancellationToken = default)
    {
        return _repository.ReportUserAsync(reporterUserId, reportedUserId, reason, cancellationToken);
    }

    public Task<bool> DeleteConversationAsync(int userId, int conversationId, CancellationToken cancellationToken = default)
    {
        return _repository.DeleteConversationForUserAsync(conversationId, userId, cancellationToken);
    }
}
