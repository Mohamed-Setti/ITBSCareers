using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class Conversation
{
    public int ConversationId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
