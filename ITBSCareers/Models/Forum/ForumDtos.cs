using System.ComponentModel.DataAnnotations;
using ITBSCareers.Models.Carriere;

namespace ITBSCareers.Models.Forum;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class ForumIndexViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string Sort { get; set; } = "recent";
    public PagedResult<ForumTopicListItemViewModel> Topics { get; set; } = new();
    public List<ForumCategory> Categories { get; set; } = new();
}

public class ForumTopicListItemViewModel
{
    public int ForumTopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int UpvotesCount { get; set; }
    public int DownvotesCount { get; set; }
    public int CommentsCount { get; set; }
    public int Score => UpvotesCount - DownvotesCount;
    public bool IsLocked { get; set; }
    public string? Excerpt { get; set; }
}

public class ForumTopicDetailsViewModel
{
    public ForumTopic Topic { get; set; } = null!;
    public string CategoryName { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool IsAdmin { get; set; }
    public bool CanComment { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public PagedResult<ForumComment> Comments { get; set; } = new();
    public List<ForumTopicHistory> TopicHistory { get; set; } = new();
}

public class ForumTopicUpsertDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public int ForumCategoryId { get; set; }
}

public class ForumCommentUpsertDto
{
    [Required]
    public string Content { get; set; } = string.Empty;
}

public class ForumCategoryUpsertDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ForumReportDto
{
    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
