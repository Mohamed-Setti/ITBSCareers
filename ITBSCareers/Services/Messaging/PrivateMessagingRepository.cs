using ITBSCareers.Models.Messaging;
using ITBSCareers.Models.Carriere;
using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Services.Messaging;

public class PrivateMessagingRepository : IPrivateMessagingRepository
{
    private readonly CarriereDbContext _context;
    private readonly MessagingPresenceTracker _presenceTracker;

    public PrivateMessagingRepository(CarriereDbContext context, MessagingPresenceTracker presenceTracker)
    {
        _context = context;
        _presenceTracker = presenceTracker;
    }

    public Task<User?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Alumni)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _context.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead && !m.Conversation.IsDeleted, cancellationToken);
    }

    public async Task<List<ConversationListItemViewModel>> GetConversationListAsync(int userId, string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.ConversationParticipants
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.Messages)
                    .ThenInclude(m => m.Sender)
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.Participants)
                    .ThenInclude(p => p.User)
            .Where(cp => cp.UserId == userId && !cp.IsArchived && !cp.Conversation.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(cp =>
                (cp.Conversation.Subject ?? string.Empty).ToLower().Contains(term) ||
                cp.Conversation.Messages.Any(m => (m.Content ?? string.Empty).ToLower().Contains(term)) ||
                cp.Conversation.Participants.Any(p => p.UserId != userId && p.User.FullName.ToLower().Contains(term)));
        }

        var conversations = await query
            .OrderByDescending(cp => cp.Conversation.Messages.Max(m => (DateTime?)m.SentAt) ?? cp.Conversation.CreatedAt)
            .ToListAsync(cancellationToken);

        return conversations.Select(cp =>
        {
            var conversation = cp.Conversation;
            var otherParticipant = conversation.Participants.FirstOrDefault(p => p.UserId != userId)?.User;
            var otherUserId = otherParticipant?.UserId ?? 0;
            var lastMessage = conversation.Messages
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefault();

            return new ConversationListItemViewModel
            {
                ConversationId = conversation.ConversationId,
                OtherUserId = otherParticipant?.UserId ?? 0,
                OtherUserName = otherParticipant?.FullName ?? "Conversation",
                IsOnline = otherUserId != 0 && _presenceTracker.IsOnline(otherUserId),
                Subject = conversation.Subject,
                LastMessage = lastMessage?.Content ?? lastMessage?.AttachmentName,
                LastMessageAt = lastMessage?.SentAt,
                UnreadCount = conversation.Messages.Count(m => m.ReceiverId == userId && !m.IsRead),
                IsBlocked = _context.PrivateUserBlocks.Any(b =>
                    (b.BlockerUserId == userId && b.BlockedUserId == otherUserId) ||
                    (b.BlockerUserId == otherUserId && b.BlockedUserId == userId))
            };
        }).ToList();
    }

    public async Task<List<MessagingContactViewModel>> GetContactsAsync(int userId, string? search, CancellationToken cancellationToken = default)
    {
        var blockedUserIds = await _context.PrivateUserBlocks
            .Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId)
            .Select(b => b.BlockerUserId == userId ? b.BlockedUserId : b.BlockerUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var term = search?.Trim().ToLowerInvariant();

        var alumniQuery = _context.Alumnis
            .AsNoTracking()
            .Include(a => a.AlumniNavigation)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Where(a => !blockedUserIds.Contains(a.AlumniId) && a.AlumniNavigation != null);

        if (!string.IsNullOrWhiteSpace(term))
        {
            alumniQuery = alumniQuery.Where(a =>
                a.AlumniNavigation!.FullName.ToLower().Contains(term) ||
                (a.CompanyName ?? string.Empty).ToLower().Contains(term) ||
                (a.Position ?? string.Empty).ToLower().Contains(term));
        }

        var alumni = await alumniQuery
            .OrderBy(a => a.AlumniNavigation!.FullName)
            .ToListAsync(cancellationToken);

        var existingConversations = await GetConversationListAsync(userId, null, cancellationToken);
        var conversationLookup = existingConversations.ToDictionary(c => c.OtherUserId, c => c);
        var contactRequests = await _context.Set<MentorshipRequest>()
            .AsNoTracking()
            .Where(m => m.StudentId == userId || m.AlumniId == userId)
            .ToListAsync(cancellationToken);

        var acceptedContactIds = contactRequests
            .Where(m => m.Status == "Accepted" || m.Status == "Approved" || m.Status == "Validated")
            .Select(m => m.StudentId == userId ? m.AlumniId : m.StudentId)
            .Distinct()
            .ToHashSet();

        var pendingOutgoingIds = contactRequests
            .Where(m => m.StudentId == userId && m.Status == "Pending")
            .Select(m => m.AlumniId)
            .ToHashSet();

        return alumni.Select(alumni =>
        {
            var user = alumni.AlumniNavigation!;
            conversationLookup.TryGetValue(user.UserId, out var conversation);
            var isPublic = alumni.IsContactPublic;
            var canMessage = isPublic || acceptedContactIds.Contains(user.UserId);
            var hasPending = pendingOutgoingIds.Contains(user.UserId);

            return new MessagingContactViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                CompanyName = alumni.CompanyName,
                Position = alumni.Position,
                RoleName = "Alumni",
                IsContactPublic = isPublic,
                IsOnline = _presenceTracker.IsOnline(user.UserId),
                ConversationId = conversation?.ConversationId,
                LastInteractionAt = conversation?.LastMessageAt,
                IsBlocked = false,
                CanMessage = canMessage,
                CanRequestContact = !canMessage && !hasPending,
                HasPendingContactRequest = hasPending,
                ActionLabel = canMessage ? (conversation == null ? "Contacter" : "Ouvrir") : hasPending ? "Demande envoyée" : "Demander contact"
            };
        }).ToList();
    }

    public async Task<ConversationViewModel?> GetConversationViewAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId, cancellationToken);

        if (conversation == null || conversation.IsDeleted)
        {
            return null;
        }

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId && !p.IsArchived);
        if (participant == null)
        {
            return null;
        }

        var otherParticipant = conversation.Participants.FirstOrDefault(p => p.UserId != userId)?.User;
        if (otherParticipant == null)
        {
            return null;
        }

        var isBlockedByCurrentUser = await _context.Set<PrivateUserBlock>()
            .AnyAsync(b => b.BlockerUserId == userId && b.BlockedUserId == otherParticipant.UserId, cancellationToken);
        var isBlockedByOtherUser = await _context.Set<PrivateUserBlock>()
            .AnyAsync(b => b.BlockerUserId == otherParticipant.UserId && b.BlockedUserId == userId, cancellationToken);
        var isBlocked = isBlockedByCurrentUser || isBlockedByOtherUser;
        var canSend = !isBlocked && await CanMessageUserAsync(userId, otherParticipant.UserId, cancellationToken);

        var messages = conversation.Messages
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageViewModel
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                SenderName = m.Sender.FullName,
                Content = m.Content,
                AttachmentPath = m.AttachmentPath,
                AttachmentName = m.AttachmentName,
                IsRead = m.IsRead,
                SentAt = m.SentAt,
                IsMine = m.SenderId == userId
            })
            .ToList();

        var conversations = await GetConversationListAsync(userId, null, cancellationToken);

        return new ConversationViewModel
        {
            ConversationId = conversation.ConversationId,
            CurrentUserId = userId,
            OtherUserId = otherParticipant.UserId,
            OtherUserName = otherParticipant.FullName,
            OtherUserRole = otherParticipant.UserRoles.FirstOrDefault(ur => ur.Role != null)?.Role?.Name,
            OtherUserIsOnline = _presenceTracker.IsOnline(otherParticipant.UserId),
            Subject = conversation.Subject,
            CanSend = canSend,
            IsBlocked = isBlocked,
            IsBlockedByCurrentUser = isBlockedByCurrentUser,
            CanUnblockUser = isBlockedByCurrentUser,
            CanDeleteConversation = true,
            CanBlockUser = true,
            CanReportUser = true,
            Messages = messages,
            Conversations = conversations
        };
    }

    public Task<Conversation?> GetConversationEntityAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        return _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId, cancellationToken);
    }

    public Task<Conversation?> FindConversationBetweenUsersAsync(int userId, int otherUserId, CancellationToken cancellationToken = default)
    {
        return _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c =>
                !c.IsDeleted &&
                c.Participants.Any(p => p.UserId == userId && !p.IsArchived) &&
                c.Participants.Any(p => p.UserId == otherUserId && !p.IsArchived), cancellationToken);
    }

    public async Task<bool> CanMessageUserAsync(int userId, int otherUserId, CancellationToken cancellationToken = default)
    {
        var otherUser = await _context.Users
            .AsNoTracking()
            .Include(u => u.Alumni)
            .FirstOrDefaultAsync(u => u.UserId == otherUserId, cancellationToken);

        if (otherUser?.Alumni?.IsContactPublic == true)
        {
            return true;
        }

        var hasContactRequest = await _context.Set<MentorshipRequest>().AnyAsync(m =>
            ((m.StudentId == userId && m.AlumniId == otherUserId) || (m.StudentId == otherUserId && m.AlumniId == userId)) &&
            (m.Status == "Accepted" || m.Status == "Approved" || m.Status == "Validated"), cancellationToken);

        return hasContactRequest;
    }

    public Task<MentorshipRequest?> GetContactRequestAsync(int studentId, int alumniId, CancellationToken cancellationToken = default)
    {
        return _context.Set<MentorshipRequest>()
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AlumniId == alumniId, cancellationToken);
    }

    public Task<MentorshipRequest?> GetContactRequestByIdAsync(int requestId, int alumniId, CancellationToken cancellationToken = default)
    {
        return _context.Set<MentorshipRequest>()
            .FirstOrDefaultAsync(r => r.MentorshipRequestId == requestId && r.AlumniId == alumniId, cancellationToken);
    }

    public async Task<MentorshipRequest> CreateContactRequestAsync(int studentId, int alumniId, CancellationToken cancellationToken = default)
    {
        var request = await GetContactRequestAsync(studentId, alumniId, cancellationToken);
        if (request != null)
        {
            if (request.Status == "Rejected")
            {
                request.Status = "Pending";
                request.CreatedAt = DateTime.Now;
                request.ReviewedAt = null;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return request;
        }

        request = new MentorshipRequest
        {
            StudentId = studentId,
            AlumniId = alumniId,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        _context.Set<MentorshipRequest>().Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task<bool> SetContactRequestStatusAsync(int requestId, int alumniId, string status, CancellationToken cancellationToken = default)
    {
        var request = await GetContactRequestByIdAsync(requestId, alumniId, cancellationToken);

        if (request == null)
        {
            return false;
        }

        request.Status = status;
        request.ReviewedAt = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> HasBlockRelationshipAsync(int userId, int otherUserId, CancellationToken cancellationToken = default)
    {
        return _context.PrivateUserBlocks.AnyAsync(b =>
            (b.BlockerUserId == userId && b.BlockedUserId == otherUserId) ||
            (b.BlockerUserId == otherUserId && b.BlockedUserId == userId), cancellationToken);
    }

    public async Task<bool> UnblockUserAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default)
    {
        var block = await _context.Set<PrivateUserBlock>()
            .FirstOrDefaultAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, cancellationToken);

        if (block == null)
        {
            return true;
        }

        _context.Set<PrivateUserBlock>().Remove(block);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Conversation> CreateConversationAsync(int creatorUserId, int otherUserId, string? subject, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation
        {
            CreatedByUserId = creatorUserId,
            Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim(),
            CreatedAt = DateTime.Now,
            IsDeleted = false
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        _context.ConversationParticipants.AddRange(
            new ConversationParticipant { ConversationId = conversation.ConversationId, UserId = creatorUserId },
            new ConversationParticipant { ConversationId = conversation.ConversationId, UserId = otherUserId });

        await _context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<Message> AddMessageAsync(int conversationId, int senderId, int receiverId, string? content, string? attachmentPath, string? attachmentName, string? attachmentContentType, long? attachmentSize, CancellationToken cancellationToken = default)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = string.IsNullOrWhiteSpace(content) ? null : content.Trim(),
            AttachmentPath = attachmentPath,
            AttachmentName = attachmentName,
            AttachmentContentType = attachmentContentType,
            AttachmentSize = attachmentSize,
            SentAt = DateTime.Now,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<bool> MarkConversationReadAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);

        if (participant == null)
        {
            return false;
        }

        participant.LastReadAt = DateTime.Now;

        var unreadMessages = await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.Now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveConversationAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);

        if (participant == null)
        {
            return false;
        }

        participant.IsArchived = true;

        var otherParticipants = await _context.ConversationParticipants
            .CountAsync(p => p.ConversationId == conversationId && !p.IsArchived && p.UserId != userId, cancellationToken);

        if (otherParticipants == 0)
        {
            var conversation = await _context.Conversations.FirstOrDefaultAsync(c => c.ConversationId == conversationId, cancellationToken);
            if (conversation != null)
            {
                conversation.IsDeleted = true;
                conversation.DeletedAt = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> BlockUserAsync(int blockerUserId, int blockedUserId, string? reason, CancellationToken cancellationToken = default)
    {
        if (blockerUserId == blockedUserId)
        {
            return false;
        }

        var exists = await _context.PrivateUserBlocks.AnyAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, cancellationToken);
        if (exists)
        {
            return true;
        }

        _context.PrivateUserBlocks.Add(new PrivateUserBlock
        {
            BlockerUserId = blockerUserId,
            BlockedUserId = blockedUserId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReportUserAsync(int reporterUserId, int reportedUserId, string reason, CancellationToken cancellationToken = default)
    {
        if (reporterUserId == reportedUserId || string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        _context.PrivateUserReports.Add(new PrivateUserReport
        {
            ReporterUserId = reporterUserId,
            ReportedUserId = reportedUserId,
            Reason = reason.Trim(),
            CreatedAt = DateTime.Now,
            IsResolved = false
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteConversationForUserAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        return await ArchiveConversationAsync(conversationId, userId, cancellationToken);
    }
}
