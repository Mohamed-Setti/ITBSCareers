using ITBSCareers.Models.Carriere;

namespace ITBSCareers.Services.Forum;

public interface IForumRepository
{
    IQueryable<ForumTopic> TopicsQuery();
    IQueryable<ForumComment> CommentsQuery();
    Task<ForumCategory?> GetCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<ForumTopic?> GetTopicAsync(int id, CancellationToken cancellationToken = default);
    Task<ForumComment?> GetCommentAsync(int id, CancellationToken cancellationToken = default);
    Task AddTopicAsync(ForumTopic topic, CancellationToken cancellationToken = default);
    Task AddCommentAsync(ForumComment comment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
