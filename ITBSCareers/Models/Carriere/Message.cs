using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class Message
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public string? Content { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
