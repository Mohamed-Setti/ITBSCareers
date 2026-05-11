using System.ComponentModel.DataAnnotations;

namespace ITBSCareers.Models.Messaging;

public class ConversationListItemViewModel
{
    public int ConversationId { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? Subject { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsBlocked { get; set; }
}

public class ConversationViewModel
{
    public int ConversationId { get; set; }
    public int CurrentUserId { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserRole { get; set; }
    public bool OtherUserIsOnline { get; set; }
    public string? Subject { get; set; }
    public bool CanSend { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsBlockedByCurrentUser { get; set; }
    public bool CanUnblockUser { get; set; }
    public bool CanDeleteConversation { get; set; }
    public bool CanBlockUser { get; set; }
    public bool CanReportUser { get; set; }
    public List<MessageViewModel> Messages { get; set; } = new();
    public List<ConversationListItemViewModel> Conversations { get; set; } = new();
}

public class MessagingIndexViewModel
{
    public int CurrentUserId { get; set; }
    public string CurrentUserName { get; set; } = string.Empty;
    public string? Search { get; set; }
    public string? ContactsSearch { get; set; }
    public int UnreadCount { get; set; }
    public List<ConversationListItemViewModel> Conversations { get; set; } = new();
    public List<MessagingContactViewModel> EligibleContacts { get; set; } = new();
    public ConversationViewModel? SelectedConversation { get; set; }
}

public class MessagingContactViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Position { get; set; }
    public string? RoleName { get; set; }
    public bool IsContactPublic { get; set; }
    public bool IsOnline { get; set; }
    public int? ConversationId { get; set; }
    public DateTime? LastInteractionAt { get; set; }
    public bool IsBlocked { get; set; }
    public bool CanMessage { get; set; }
    public bool CanRequestContact { get; set; }
    public bool HasPendingContactRequest { get; set; }
    public string ActionLabel { get; set; } = "Contacter";
}

public class ContactRequestViewModel
{
    public int RequestId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class MessageViewModel
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }
    public bool IsRead { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsMine { get; set; }
}

public class StartConversationDto
{
    [Required]
    public int OtherUserId { get; set; }

    [StringLength(200)]
    public string? Subject { get; set; }
}

public class SendMessageDto
{
    [StringLength(4000)]
    public string? Content { get; set; }
}

public class BlockUserDto
{
    [StringLength(500)]
    public string? Reason { get; set; }
}

public class ReportUserDto
{
    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
