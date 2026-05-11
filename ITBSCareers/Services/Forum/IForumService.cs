using ITBSCareers.Models.Carriere;
using ITBSCareers.Models.Forum;

namespace ITBSCareers.Services.Forum;

public interface IForumService
{
    Task<ForumIndexViewModel> GetTopicsAsync(string? search, int? categoryId, string sort, int page, int pageSize, int? currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<ForumTopicDetailsViewModel?> GetTopicDetailsAsync(int topicId, int page, int pageSize, int? currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<ForumTopic?> CreateTopicAsync(int userId, ForumTopicUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateTopicAsync(int topicId, int userId, bool isAdmin, ForumTopicUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteTopicAsync(int topicId, int userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<ForumComment?> CreateCommentAsync(int topicId, int userId, ForumCommentUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateCommentAsync(int commentId, int userId, bool isAdmin, ForumCommentUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCommentAsync(int commentId, int userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<(bool success, int score, int upvotes, int downvotes)> VoteTopicAsync(int topicId, int userId, bool isUpvote, CancellationToken cancellationToken = default);
    Task<(bool success, int score, int upvotes, int downvotes)> VoteCommentAsync(int commentId, int userId, bool isUpvote, CancellationToken cancellationToken = default);
    Task<bool> ReportTopicAsync(int topicId, int userId, ForumReportDto dto, CancellationToken cancellationToken = default);
    Task<bool> ReportCommentAsync(int commentId, int userId, ForumReportDto dto, CancellationToken cancellationToken = default);
    Task<bool> LockTopicAsync(int topicId, int userId, bool isAdmin, bool locked, CancellationToken cancellationToken = default);
    Task<bool> BanUserAsync(int userIdToBan, int bannedByUserId, string? reason, DateTime? endsAt, CancellationToken cancellationToken = default);
    Task<bool> UnbanUserAsync(int userIdToUnban, CancellationToken cancellationToken = default);
    Task<List<ForumCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<List<ForumCategory>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ForumCategory?> CreateCategoryAsync(ForumCategoryUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateCategoryAsync(int id, ForumCategoryUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<ForumReport>> GetReportsAsync(bool resolved, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ResolveReportAsync(int reportId, int resolvedByUserId, CancellationToken cancellationToken = default);
    Task<bool> IsUserBannedAsync(int userId, CancellationToken cancellationToken = default);
}
