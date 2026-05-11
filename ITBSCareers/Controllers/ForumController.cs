using System.Security.Claims;
using ITBSCareers.Models.Carriere;
using ITBSCareers.Models.Forum;
using ITBSCareers.Services.Forum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBSCareers.Controllers;

[Authorize]
public class ForumController : Controller
{
    private readonly IForumService _forumService;

    public ForumController(IForumService forumService)
    {
        _forumService = forumService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, int? categoryId, string sort = "recent", int page = 1, int pageSize = 10)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var vm = await _forumService.GetTopicsAsync(search, categoryId, sort, page, pageSize, currentUserId, isAdmin);
        return View(vm);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, int page = 1, int pageSize = 10)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var vm = await _forumService.GetTopicDetailsAsync(id, page, pageSize, currentUserId, isAdmin);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _forumService.GetActiveCategoriesAsync();
        return View(new ForumTopicUpsertDto());
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ForumTopicUpsertDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _forumService.GetActiveCategoriesAsync();
            return View(dto);
        }

        var userId = RequireCurrentUserId();
        if (await _forumService.IsUserBannedAsync(userId))
        {
            return Forbid();
        }

        var topic = await _forumService.CreateTopicAsync(userId, dto);
        if (topic == null)
        {
            ModelState.AddModelError(string.Empty, "Thématique invalide.");
            ViewBag.Categories = await _forumService.GetActiveCategoriesAsync();
            return View(dto);
        }

        return RedirectToAction(nameof(Details), new { id = topic.ForumTopicId });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    public async Task<IActionResult> EditTopic(int id)
    {
        var topic = await _forumService.GetTopicDetailsAsync(id, 1, 1, GetCurrentUserId(), User.IsInRole("Admin"), default);
        if (topic == null) return NotFound();
        if (!topic.IsOwner && !topic.IsAdmin) return Forbid();

        ViewBag.TopicId = id;
        ViewBag.Categories = await _forumService.GetCategoriesAsync();
        return View(new ForumTopicUpsertDto
        {
            Title = topic.Topic.Title,
            Content = topic.Topic.Content,
            ForumCategoryId = topic.Topic.ForumCategoryId
        });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTopic(int id, ForumTopicUpsertDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TopicId = id;
            ViewBag.Categories = await _forumService.GetCategoriesAsync();
            return View(dto);
        }

        var userId = RequireCurrentUserId();
        var updated = await _forumService.UpdateTopicAsync(id, userId, User.IsInRole("Admin"), dto);
        if (!updated) return Forbid();
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTopic(int id)
    {
        var userId = RequireCurrentUserId();
        var deleted = await _forumService.DeleteTopicAsync(id, userId, User.IsInRole("Admin"));
        if (!deleted) return Forbid();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComment(int topicId, ForumCommentUpsertDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["ForumError"] = "Le commentaire ne peut pas être vide.";
            return RedirectToAction(nameof(Details), new { id = topicId });
        }

        var userId = RequireCurrentUserId();
        if (await _forumService.IsUserBannedAsync(userId)) return Forbid();

        var comment = await _forumService.CreateCommentAsync(topicId, userId, dto);
        if (comment == null)
        {
            TempData["ForumError"] = "Impossible d'ajouter un commentaire sur ce sujet.";
        }

        return RedirectToAction(nameof(Details), new { id = topicId });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(int id, int topicId, ForumCommentUpsertDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["ForumError"] = "Le commentaire ne peut pas être vide.";
            return RedirectToAction(nameof(Details), new { id = topicId });
        }

        var userId = RequireCurrentUserId();
        var updated = await _forumService.UpdateCommentAsync(id, userId, User.IsInRole("Admin"), dto);
        if (!updated) return Forbid();
        return RedirectToAction(nameof(Details), new { id = topicId });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, int topicId)
    {
        var userId = RequireCurrentUserId();
        var deleted = await _forumService.DeleteCommentAsync(id, userId, User.IsInRole("Admin"));
        if (!deleted) return Forbid();
        return RedirectToAction(nameof(Details), new { id = topicId });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoteTopic(int topicId, bool isUpvote)
    {
        var result = await _forumService.VoteTopicAsync(topicId, RequireCurrentUserId(), isUpvote);
        return Json(new { success = result.success, score = result.score, upvotes = result.upvotes, downvotes = result.downvotes });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoteComment(int commentId, bool isUpvote)
    {
        var result = await _forumService.VoteCommentAsync(commentId, RequireCurrentUserId(), isUpvote);
        return Json(new { success = result.success, score = result.score, upvotes = result.upvotes, downvotes = result.downvotes });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportTopic(int topicId, ForumReportDto dto)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Details), new { id = topicId });
        var ok = await _forumService.ReportTopicAsync(topicId, RequireCurrentUserId(), dto);
        TempData["ForumMessage"] = ok ? "Sujet signalé." : "Signalement impossible.";
        return RedirectToAction(nameof(Details), new { id = topicId });
    }

    [Authorize(Roles = "Student,Alumni,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportComment(int commentId, int topicId, ForumReportDto dto)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Details), new { id = topicId });
        var ok = await _forumService.ReportCommentAsync(commentId, RequireCurrentUserId(), dto);
        TempData["ForumMessage"] = ok ? "Commentaire signalé." : "Signalement impossible.";
        return RedirectToAction(nameof(Details), new { id = topicId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockTopic(int id, bool locked)
    {
        var ok = await _forumService.LockTopicAsync(id, RequireCurrentUserId(), true, locked);
        if (!ok) return NotFound();
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Categories()
    {
        var categories = await _forumService.GetCategoriesAsync();
        return View(categories);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult CreateCategory() => View(new ForumCategoryUpsertDto());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(ForumCategoryUpsertDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var category = await _forumService.CreateCategoryAsync(dto);
        if (category == null) return View(dto);
        return RedirectToAction(nameof(Categories));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditCategory(int id)
    {
        var category = (await _forumService.GetCategoriesAsync()).FirstOrDefault(c => c.ForumCategoryId == id);
        if (category == null) return NotFound();
        ViewBag.CategoryId = id;
        return View(new ForumCategoryUpsertDto { Name = category.Name, Description = category.Description, IsActive = category.IsActive });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, ForumCategoryUpsertDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CategoryId = id;
            return View(dto);
        }
        var ok = await _forumService.UpdateCategoryAsync(id, dto);
        if (!ok) return NotFound();
        return RedirectToAction(nameof(Categories));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var ok = await _forumService.DeleteCategoryAsync(id);
        if (!ok) TempData["ForumMessage"] = "La thématique contient encore des sujets actifs.";
        return RedirectToAction(nameof(Categories));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reports(bool resolved = false, int page = 1, int pageSize = 10)
    {
        var reports = await _forumService.GetReportsAsync(resolved, page, pageSize);
        ViewBag.Resolved = resolved;
        return View(reports);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveReport(int reportId, bool resolved)
    {
        if (resolved)
        {
            var ok = await _forumService.ResolveReportAsync(reportId, RequireCurrentUserId());
            if (!ok) return NotFound();
        }
        return RedirectToAction(nameof(Reports));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanUser(int userIdToBan, string? reason, int? days)
    {
        DateTime? endsAt = days.HasValue && days.Value > 0 ? DateTime.Now.AddDays(days.Value) : (DateTime?)null;
        await _forumService.BanUserAsync(userIdToBan, RequireCurrentUserId(), reason, endsAt);
        return RedirectToAction(nameof(Reports));
    }

    private int RequireCurrentUserId()
    {
        var id = GetCurrentUserId();
        if (!id.HasValue) throw new UnauthorizedAccessException();
        return id.Value;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claim, out var id)) return id;
        return HttpContext.Session.GetInt32("UserId");
    }
}
