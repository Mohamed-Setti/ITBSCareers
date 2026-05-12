using System.Security.Claims;
using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
using ITBSCareers.Services.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers;

[Authorize(Roles = "Student")]
public class AlumniDirectoryController : Controller
{
    private readonly CarriereDbContext _context;
    private readonly IPrivateMessagingService _messagingService;

    public AlumniDirectoryController(CarriereDbContext context, IPrivateMessagingService messagingService)
    {
        _context = context;
        _messagingService = messagingService;
    }

    public async Task<IActionResult> Index(string? query, string? visibility = "All")
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "User");
        }

        var alumnis = await _context.Alumnis
            .AsNoTracking()
            .Include(a => a.AlumniNavigation)
                .ThenInclude(u => u.Student)
                    .ThenInclude(s => s.Degree)
            .ToListAsync();

        var acceptedContactIds = await _context.MentorshipRequests
            .AsNoTracking()
            .Where(r => r.StudentId == userId.Value && (r.Status == "Accepted" || r.Status == "Approved" || r.Status == "Validated"))
            .Select(r => r.AlumniId)
            .ToListAsync();

        var pendingContactIds = await _context.MentorshipRequests
            .AsNoTracking()
            .Where(r => r.StudentId == userId.Value && r.Status == "Pending")
            .Select(r => r.AlumniId)
            .ToListAsync();

        var conversationLookup = await _context.ConversationParticipants
            .AsNoTracking()
            .Where(cp => cp.UserId == userId.Value && !cp.Conversation.IsDeleted)
            .SelectMany(cp => cp.Conversation.Participants.Where(p => p.UserId != userId.Value))
            .Select(p => new { p.UserId, p.ConversationId })
            .ToListAsync();

        var conversationMap = conversationLookup
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().ConversationId);

        string? normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var currentVisibility = string.IsNullOrWhiteSpace(visibility) ? "All" : visibility.Trim();

        var items = alumnis
            .Where(a => a.AlumniNavigation != null)
            .Select(a =>
            {
                var user = a.AlumniNavigation!;
                var degreeName = user.Student?.Degree?.Name;
                var field = user.Student?.Field;
                var specialityLabel = !string.IsNullOrWhiteSpace(field)
                    ? field!
                    : !string.IsNullOrWhiteSpace(degreeName)
                        ? degreeName!
                        : a.Position ?? "Alumni";

                var initialsParts = (user.FullName ?? "A").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = string.Concat(initialsParts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
                if (string.IsNullOrWhiteSpace(initials))
                {
                    initials = "A";
                }

                var canMessage = a.IsContactPublic || acceptedContactIds.Contains(a.AlumniId);
                var hasPending = pendingContactIds.Contains(a.AlumniId);
                int? conversationId = null;
                if (conversationMap.TryGetValue(a.AlumniId, out var existingConversationId))
                {
                    conversationId = existingConversationId;
                }

                return new AlumniDirectoryItemViewModel
                {
                    AlumniId = a.AlumniId,
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    DegreeName = degreeName,
                    Field = field,
                    CompanyName = a.CompanyName,
                    Position = a.Position,
                    ExperienceYears = a.ExperienceYears,
                    IsContactPublic = a.IsContactPublic,
                    CanMessage = canMessage,
                    HasPendingContactRequest = hasPending,
                    ConversationId = conversationId,
                    ActionLabel = hasPending ? "Demande envoyée" : conversationId.HasValue && canMessage ? "Ouvrir la discussion" : a.IsContactPublic ? "Contacter" : "Demander contact",
                    StatusLabel = a.IsContactPublic ? "Public" : "Privé",
                    SpecialityLabel = specialityLabel,
                    Initials = initials
                };
            })
            .Where(item => string.IsNullOrWhiteSpace(normalizedQuery)
                || item.FullName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || (item.CompanyName ?? string.Empty).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || (item.Position ?? string.Empty).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || (item.DegreeName ?? string.Empty).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || (item.Field ?? string.Empty).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .Where(item => currentVisibility.ToLowerInvariant() switch
            {
                "public" => item.IsContactPublic,
                "private" => !item.IsContactPublic,
                _ => true
            })
            .OrderBy(item => item.FullName)
            .ToList();

        var model = new AlumniDirectoryViewModel
        {
            Query = query,
            Visibility = currentVisibility,
            Alumni = items,
            TotalCount = items.Count,
            PublicCount = items.Count(x => x.IsContactPublic),
            PrivateCount = items.Count(x => !x.IsContactPublic),
            PendingContactRequestsCount = items.Count(x => x.HasPendingContactRequest)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(int alumniId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "User");
        }

        var alumni = await _context.Alumnis
            .AsNoTracking()
            .Include(a => a.AlumniNavigation)
            .FirstOrDefaultAsync(a => a.AlumniId == alumniId);

        if (alumni?.AlumniNavigation == null)
        {
            return NotFound();
        }

        var startResult = await _messagingService.StartConversationAsync(userId.Value, alumniId, null);
        if (startResult.success)
        {
            return RedirectToAction("Index", "Messaging", new { conversationId = startResult.conversationId });
        }

        var requestResult = await _messagingService.RequestContactAsync(userId.Value, alumniId);
        if (requestResult.success && requestResult.pending)
        {
            TempData["Message"] = $"Une demande de contact a été envoyée à {alumni.AlumniNavigation.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Message"] = requestResult.error ?? startResult.error ?? "Impossible de contacter cet alumni.";
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claimValue, out var id))
        {
            return id;
        }

        return HttpContext.Session.GetInt32("UserId");
    }
}
