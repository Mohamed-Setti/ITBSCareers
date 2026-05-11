namespace ITBSCareers.Models.Carriere;

public partial class User
{
    public virtual ICollection<ForumTopic> ForumTopics { get; set; } = new List<ForumTopic>();
    public virtual ICollection<ForumComment> ForumComments { get; set; } = new List<ForumComment>();
    public virtual ICollection<ForumVote> ForumVotes { get; set; } = new List<ForumVote>();
    public virtual ICollection<ForumReport> ForumReports { get; set; } = new List<ForumReport>();
    public virtual ICollection<ForumReport> ForumResolvedReports { get; set; } = new List<ForumReport>();
    public virtual ICollection<ForumUserBan> ForumBans { get; set; } = new List<ForumUserBan>();
    public virtual ICollection<ForumUserBan> ForumBansIssued { get; set; } = new List<ForumUserBan>();
}
