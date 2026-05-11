using ITBSCareers.Models.Carriere;
using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Services.Forum;

public class ForumRepository : IForumRepository
{
    private readonly CarriereDbContext _context;

    public ForumRepository(CarriereDbContext context)
    {
        _context = context;
    }

    public IQueryable<ForumTopic> TopicsQuery() => _context.ForumTopics.AsNoTracking();

    public IQueryable<ForumComment> CommentsQuery() => _context.ForumComments.AsNoTracking();

    public Task<ForumCategory?> GetCategoryAsync(int id, CancellationToken cancellationToken = default)
        => _context.ForumCategories.FirstOrDefaultAsync(c => c.ForumCategoryId == id, cancellationToken);

    public Task<ForumTopic?> GetTopicAsync(int id, CancellationToken cancellationToken = default)
        => _context.ForumTopics
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.CreatedBy)
            .Include(t => t.Histories)
                .ThenInclude(h => h.ChangedBy)
            .FirstOrDefaultAsync(t => t.ForumTopicId == id && !t.IsDeleted, cancellationToken);

    public Task<ForumComment?> GetCommentAsync(int id, CancellationToken cancellationToken = default)
        => _context.ForumComments
            .Include(c => c.Topic)
            .FirstOrDefaultAsync(c => c.ForumCommentId == id && !c.IsDeleted, cancellationToken);

    public async Task AddTopicAsync(ForumTopic topic, CancellationToken cancellationToken = default)
        => await _context.ForumTopics.AddAsync(topic, cancellationToken);

    public async Task AddCommentAsync(ForumComment comment, CancellationToken cancellationToken = default)
        => await _context.ForumComments.AddAsync(comment, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
