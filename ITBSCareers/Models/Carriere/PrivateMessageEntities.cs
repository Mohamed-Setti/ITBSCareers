using System.ComponentModel.DataAnnotations;

namespace ITBSCareers.Models.Carriere;

public partial class Conversation
{
    public int ConversationId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string? Subject { get; set; }

    public virtual User CreatedBy { get; set; } = null!;
    public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}

public partial class ConversationParticipant
{
    public int ConversationParticipantId { get; set; }
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.Now;
    public DateTime? LastReadAt { get; set; }
    public bool IsArchived { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public partial class Message
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string? Content { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentContentType { get; set; }
    public long? AttachmentSize { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? SentAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User Receiver { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
}

public partial class PrivateUserBlock
{
    public int PrivateUserBlockId { get; set; }
    public int BlockerUserId { get; set; }
    public int BlockedUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual User Blocker { get; set; } = null!;
    public virtual User Blocked { get; set; } = null!;
}

public partial class PrivateUserReport
{
    public int PrivateUserReportId { get; set; }
    public int ReporterUserId { get; set; }
    public int ReportedUserId { get; set; }
    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public int? ResolvedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ResolvedAt { get; set; }

    public virtual User Reporter { get; set; } = null!;
    public virtual User Reported { get; set; } = null!;
    public virtual User? ResolvedBy { get; set; }
}
