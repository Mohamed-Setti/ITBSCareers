namespace ITBSCareers.Models.Carriere;

public partial class User
{
    public virtual ICollection<Conversation> CreatedConversations { get; set; } = new List<Conversation>();
    public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
    public virtual ICollection<PrivateUserBlock> BlocksIssued { get; set; } = new List<PrivateUserBlock>();
    public virtual ICollection<PrivateUserBlock> BlocksReceived { get; set; } = new List<PrivateUserBlock>();
    public virtual ICollection<PrivateUserReport> PrivateReportsIssued { get; set; } = new List<PrivateUserReport>();
    public virtual ICollection<PrivateUserReport> PrivateReportsReceived { get; set; } = new List<PrivateUserReport>();
    public virtual ICollection<PrivateUserReport> PrivateReportsResolved { get; set; } = new List<PrivateUserReport>();
}
