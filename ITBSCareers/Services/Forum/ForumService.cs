using ITBSCareers.Models.Carriere;
using ITBSCareers.Models.Forum;
using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Services.Forum;

public class ForumService : IForumService
{
    private readonly CarriereDbContext _context;
    private readonly IForumRepository _repository;

    public ForumService(CarriereDbContext context, IForumRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    public async Task<List<ForumCategory>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
        => await _context.ForumCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task<List<ForumCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await _context.ForumCategories.OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task<ForumIndexViewModel> GetTopicsAsync(string? search, int? categoryId, string sort, int page, int pageSize, int? currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var query = _context.ForumTopics
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.ForumCategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(t => t.Title.Contains(s) || t.Content.Contains(s) || t.Category.Name.Contains(s));
        }

        query = sort switch
        {
            "popular" => query.OrderByDescending(t => t.UpvotesCount - t.DownvotesCount).ThenByDescending(t => t.CreatedAt),
            "commented" => query.OrderByDescending(t => t.CommentsCount).ThenByDescending(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new ForumTopicListItemViewModel
            {
                ForumTopicId = t.ForumTopicId,
                Title = t.Title,
                CategoryName = t.Category.Name,
                AuthorName = t.CreatedBy.FullName,
                CreatedAt = t.CreatedAt,
                UpvotesCount = t.UpvotesCount,
                DownvotesCount = t.DownvotesCount,
                CommentsCount = t.CommentsCount,
                IsLocked = t.IsLocked,
                Excerpt = t.Content.Length > 180 ? t.Content.Substring(0, 180) + "..." : t.Content
            })
            .ToListAsync(cancellationToken);

        return new ForumIndexViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Sort = sort,
            Categories = await GetActiveCategoriesAsync(cancellationToken),
            Topics = new PagedResult<ForumTopicListItemViewModel>
            {
                Items = items,
                PageIndex = page,
                PageSize = pageSize,
                TotalCount = total
            }
        };
    }

    public async Task<ForumTopicDetailsViewModel?> GetTopicDetailsAsync(int topicId, int page, int pageSize, int? currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var topic = await _repository.GetTopicAsync(topicId, cancellationToken);
        if (topic == null) return null;

        var commentsQuery = _context.ForumComments
            .Include(c => c.CreatedBy)
            .Where(c => c.ForumTopicId == topicId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        var totalComments = await commentsQuery.CountAsync(cancellationToken);
        var comments = await commentsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ForumTopicDetailsViewModel
        {
            Topic = topic,
            CategoryName = topic.Category.Name,
            AuthorName = topic.CreatedBy.FullName,
            IsOwner = currentUserId.HasValue && topic.CreatedByUserId == currentUserId.Value,
            IsAdmin = isAdmin,
            CanComment = !topic.IsLocked,
            PageIndex = page,
            PageSize = pageSize,
            Comments = new PagedResult<ForumComment>
            {
                Items = comments,
                PageIndex = page,
                PageSize = pageSize,
                TotalCount = totalComments
            },
            TopicHistory = topic.Histories.OrderByDescending(h => h.ChangedAt).ToList()
        };
    }

    public async Task<ForumTopic?> CreateTopicAsync(int userId, ForumTopicUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetCategoryAsync(dto.ForumCategoryId, cancellationToken);
        if (category == null || !category.IsActive) return null;

        var topic = new ForumTopic
        {
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            ForumCategoryId = dto.ForumCategoryId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _repository.AddTopicAsync(topic, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return topic;
    }

    public async Task<bool> UpdateTopicAsync(int topicId, int userId, bool isAdmin, ForumTopicUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var topic = await _context.ForumTopics.Include(t => t.Histories).FirstOrDefaultAsync(t => t.ForumTopicId == topicId && !t.IsDeleted, cancellationToken);
        if (topic == null) return false;
        if (!isAdmin && topic.CreatedByUserId != userId) return false;

        topic.Histories.Add(new ForumTopicHistory
        {
            ForumTopicId = topic.ForumTopicId,
            ChangedByUserId = userId,
            TitleSnapshot = topic.Title,
            ContentSnapshot = topic.Content,
            ChangedAt = DateTime.Now
        });

        topic.Title = dto.Title.Trim();
        topic.Content = dto.Content.Trim();
        topic.ForumCategoryId = dto.ForumCategoryId;
        topic.UpdatedAt = DateTime.Now;

        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteTopicAsync(int topicId, int userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var topic = await _context.ForumTopics.Include(t => t.Comments).FirstOrDefaultAsync(t => t.ForumTopicId == topicId, cancellationToken);
        if (topic == null) return false;
        if (!isAdmin && topic.CreatedByUserId != userId) return false;

        topic.IsDeleted = true;
        topic.UpdatedAt = DateTime.Now;
        foreach (var c in topic.Comments) c.IsDeleted = true;
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ForumComment?> CreateCommentAsync(int topicId, int userId, ForumCommentUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.ForumTopicId == topicId && !t.IsDeleted, cancellationToken);
        if (topic == null || topic.IsLocked) return null;

        var comment = new ForumComment
        {
            ForumTopicId = topicId,
            CreatedByUserId = userId,
            Content = dto.Content.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _repository.AddCommentAsync(comment, cancellationToken);
        topic.CommentsCount += 1;
        await _repository.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task<bool> UpdateCommentAsync(int commentId, int userId, bool isAdmin, ForumCommentUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var comment = await _context.ForumComments.Include(c => c.Histories).FirstOrDefaultAsync(c => c.ForumCommentId == commentId && !c.IsDeleted, cancellationToken);
        if (comment == null) return false;
        if (!isAdmin && comment.CreatedByUserId != userId) return false;

        comment.Histories.Add(new ForumCommentHistory
        {
            ForumCommentId = comment.ForumCommentId,
            ChangedByUserId = userId,
            ContentSnapshot = comment.Content,
            ChangedAt = DateTime.Now
        });

        comment.Content = dto.Content.Trim();
        comment.UpdatedAt = DateTime.Now;
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var comment = await _context.ForumComments.FirstOrDefaultAsync(c => c.ForumCommentId == commentId && !c.IsDeleted, cancellationToken);
        if (comment == null) return false;
        if (!isAdmin && comment.CreatedByUserId != userId) return false;

        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.Now;
        var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.ForumTopicId == comment.ForumTopicId, cancellationToken);
        if (topic != null && topic.CommentsCount > 0) topic.CommentsCount -= 1;
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool success, int score, int upvotes, int downvotes)> VoteTopicAsync(int topicId, int userId, bool isUpvote, CancellationToken cancellationToken = default)
    {
        var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.ForumTopicId == topicId && !t.IsDeleted, cancellationToken);
        if (topic == null) return (false, 0, 0, 0);

        var existing = await _context.ForumVotes.FirstOrDefaultAsync(v => v.ForumTopicId == topicId && v.UserId == userId, cancellationToken);
        if (existing == null)
        {
            _context.ForumVotes.Add(new ForumVote { ForumTopicId = topicId, UserId = userId, IsUpvote = isUpvote });
            if (isUpvote) topic.UpvotesCount++; else topic.DownvotesCount++;
        }
        else if (existing.IsUpvote != isUpvote)
        {
            if (existing.IsUpvote) topic.UpvotesCount--; else topic.DownvotesCount--;
            existing.IsUpvote = isUpvote;
            if (isUpvote) topic.UpvotesCount++; else topic.DownvotesCount++;
        }
        else
        {
            return (true, topic.UpvotesCount - topic.DownvotesCount, topic.UpvotesCount, topic.DownvotesCount);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return (true, topic.UpvotesCount - topic.DownvotesCount, topic.UpvotesCount, topic.DownvotesCount);
    }

    public async Task<(bool success, int score, int upvotes, int downvotes)> VoteCommentAsync(int commentId, int userId, bool isUpvote, CancellationToken cancellationToken = default)
    {
        var comment = await _context.ForumComments.FirstOrDefaultAsync(c => c.ForumCommentId == commentId && !c.IsDeleted, cancellationToken);
        if (comment == null) return (false, 0, 0, 0);

        var existing = await _context.ForumVotes.FirstOrDefaultAsync(v => v.ForumCommentId == commentId && v.UserId == userId, cancellationToken);
        if (existing == null)
        {
            _context.ForumVotes.Add(new ForumVote { ForumCommentId = commentId, ForumTopicId = comment.ForumTopicId, UserId = userId, IsUpvote = isUpvote });
            if (isUpvote) comment.UpvotesCount++; else comment.DownvotesCount++;
        }
        else if (existing.IsUpvote != isUpvote)
        {
            if (existing.IsUpvote) comment.UpvotesCount--; else comment.DownvotesCount--;
            existing.IsUpvote = isUpvote;
            if (isUpvote) comment.UpvotesCount++; else comment.DownvotesCount++;
        }
        else
        {
            return (true, comment.UpvotesCount - comment.DownvotesCount, comment.UpvotesCount, comment.DownvotesCount);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return (true, comment.UpvotesCount - comment.DownvotesCount, comment.UpvotesCount, comment.DownvotesCount);
    }

    public async Task<bool> ReportTopicAsync(int topicId, int userId, ForumReportDto dto, CancellationToken cancellationToken = default)
    {
        var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.ForumTopicId == topicId && !t.IsDeleted, cancellationToken);
        if (topic == null) return false;

        _context.ForumReports.Add(new ForumReport
        {
            ForumTopicId = topicId,
            ReportedByUserId = userId,
            Reason = dto.Reason.Trim(),
            CreatedAt = DateTime.Now
        });
        topic.ReportsCount += 1;
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReportCommentAsync(int commentId, int userId, ForumReportDto dto, CancellationToken cancellationToken = default)
    {
        var comment = await _context.ForumComments.FirstOrDefaultAsync(c => c.ForumCommentId == commentId && !c.IsDeleted, cancellationToken);
        if (comment == null) return false;

        _context.ForumReports.Add(new ForumReport
        {
            ForumTopicId = comment.ForumTopicId,
            ForumCommentId = commentId,
            ReportedByUserId = userId,
            Reason = dto.Reason.Trim(),
            CreatedAt = DateTime.Now
        });
        comment.ReportsCount += 1;
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> LockTopicAsync(int topicId, int userId, bool isAdmin, bool locked, CancellationToken cancellationToken = default)
    {
        var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.ForumTopicId == topicId && !t.IsDeleted, cancellationToken);
        if (topic == null) return false;
        if (!isAdmin && topic.CreatedByUserId != userId) return false;

        topic.IsLocked = locked;
        topic.UpdatedAt = DateTime.Now;
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> BanUserAsync(int userIdToBan, int bannedByUserId, string? reason, DateTime? endsAt, CancellationToken cancellationToken = default)
    {
        var ban = await _context.ForumUserBans.FirstOrDefaultAsync(b => b.UserId == userIdToBan && b.IsActive, cancellationToken);
        if (ban != null)
        {
            ban.Reason = reason;
            ban.EndsAt = endsAt;
            ban.BannedByUserId = bannedByUserId;
            ban.IsActive = true;
            await _repository.SaveChangesAsync(cancellationToken);
            return true;
        }

        _context.ForumUserBans.Add(new ForumUserBan
        {
            UserId = userIdToBan,
            BannedByUserId = bannedByUserId,
            Reason = reason,
            EndsAt = endsAt,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnbanUserAsync(int userIdToUnban, CancellationToken cancellationToken = default)
    {
        var activeBan = await _context.ForumUserBans.FirstOrDefaultAsync(b => b.UserId == userIdToUnban && b.IsActive, cancellationToken);
        if (activeBan == null) return false;
        activeBan.IsActive = false;
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ForumCategory?> CreateCategoryAsync(ForumCategoryUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var exists = await _context.ForumCategories.AnyAsync(c => c.Name == dto.Name.Trim(), cancellationToken);
        if (exists) return null;
        var category = new ForumCategory { Name = dto.Name.Trim(), Description = dto.Description?.Trim(), IsActive = dto.IsActive, CreatedAt = DateTime.Now };
        _context.ForumCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<bool> UpdateCategoryAsync(int id, ForumCategoryUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _context.ForumCategories.FirstOrDefaultAsync(c => c.ForumCategoryId == id, cancellationToken);
        if (category == null) return false;
        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
        category.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _context.ForumCategories.Include(c => c.Topics).FirstOrDefaultAsync(c => c.ForumCategoryId == id, cancellationToken);
        if (category == null) return false;
        if (category.Topics.Any(t => !t.IsDeleted)) return false;
        _context.ForumCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<ForumReport>> GetReportsAsync(bool resolved, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ForumReports
            .Include(r => r.Topic)
            .Include(r => r.Comment)
            .Include(r => r.ReportedBy)
            .Include(r => r.ResolvedBy)
            .Where(r => r.IsResolved == resolved)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<ForumReport> { Items = items, PageIndex = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<bool> ResolveReportAsync(int reportId, int resolvedByUserId, CancellationToken cancellationToken = default)
    {
        var report = await _context.ForumReports.FirstOrDefaultAsync(r => r.ForumReportId == reportId, cancellationToken);
        if (report == null) return false;
        report.IsResolved = true;
        report.ResolvedByUserId = resolvedByUserId;
        report.ResolvedAt = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> IsUserBannedAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        return await _context.ForumUserBans.AnyAsync(b => b.UserId == userId && b.IsActive && (b.EndsAt == null || b.EndsAt > now), cancellationToken);
    }
}
