using System.ComponentModel.DataAnnotations;

namespace ITBSCareers.Models.Carriere;

public partial class ForumCategory
{
    public int ForumCategoryId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<ForumTopic> Topics { get; set; } = new List<ForumTopic>();
}

public partial class ForumTopic
{
    public int ForumTopicId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public int ForumCategoryId { get; set; }
    public int CreatedByUserId { get; set; }

    public bool IsLocked { get; set; }
    public bool IsDeleted { get; set; }

    public int UpvotesCount { get; set; }
    public int DownvotesCount { get; set; }
    public int CommentsCount { get; set; }
    public int ReportsCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual ForumCategory Category { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
    public virtual ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>();
    public virtual ICollection<ForumVote> Votes { get; set; } = new List<ForumVote>();
    public virtual ICollection<ForumReport> Reports { get; set; } = new List<ForumReport>();
    public virtual ICollection<ForumTopicHistory> Histories { get; set; } = new List<ForumTopicHistory>();
}

public partial class ForumComment
{
    public int ForumCommentId { get; set; }

    public int ForumTopicId { get; set; }
    public int CreatedByUserId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public int UpvotesCount { get; set; }
    public int DownvotesCount { get; set; }
    public int ReportsCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual ForumTopic Topic { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
    public virtual ICollection<ForumCommentHistory> Histories { get; set; } = new List<ForumCommentHistory>();
}

public partial class ForumVote
{
    public int ForumVoteId { get; set; }
    public int ForumTopicId { get; set; }
    public int? ForumCommentId { get; set; }
    public int UserId { get; set; }
    public bool IsUpvote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ForumTopic Topic { get; set; } = null!;
    public virtual ForumComment? Comment { get; set; }
    public virtual User User { get; set; } = null!;
}

public partial class ForumReport
{
    public int ForumReportId { get; set; }
    public int ForumTopicId { get; set; }
    public int? ForumCommentId { get; set; }
    public int ReportedByUserId { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public bool IsResolved { get; set; }
    public int? ResolvedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ResolvedAt { get; set; }

    public virtual ForumTopic Topic { get; set; } = null!;
    public virtual ForumComment? Comment { get; set; }
    public virtual User ReportedBy { get; set; } = null!;
    public virtual User? ResolvedBy { get; set; }
}

public partial class ForumTopicHistory
{
    public int ForumTopicHistoryId { get; set; }
    public int ForumTopicId { get; set; }
    public int ChangedByUserId { get; set; }
    public string TitleSnapshot { get; set; } = string.Empty;
    public string ContentSnapshot { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    public virtual ForumTopic Topic { get; set; } = null!;
    public virtual User ChangedBy { get; set; } = null!;
}

public partial class ForumCommentHistory
{
    public int ForumCommentHistoryId { get; set; }
    public int ForumCommentId { get; set; }
    public int ChangedByUserId { get; set; }
    public string ContentSnapshot { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    public virtual ForumComment Comment { get; set; } = null!;
    public virtual User ChangedBy { get; set; } = null!;
}

public partial class ForumUserBan
{
    public int ForumUserBanId { get; set; }
    public int UserId { get; set; }
    public int BannedByUserId { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? EndsAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual User BannedBy { get; set; } = null!;
}
