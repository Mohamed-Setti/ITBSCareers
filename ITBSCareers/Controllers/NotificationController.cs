using System.Security.Claims;
using System.Text.Json;
using ITBSCareers.Models.Carriere;
using ITBSCareers.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly CarriereDbContext _context;
        private readonly INotificationService _notificationService;

        public NotificationController(CarriereDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var notifications = await _notificationService.GetRecentAsync(userId.Value, 50);

            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> Recent(int count = 5)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var notifications = await _notificationService.GetRecentAsync(userId.Value, count);

            var payload = notifications.Select(n => new
            {
                n.NotificationId,
                n.Type,
                n.Content,
                n.IsRead,
                n.CreatedAt
            });

            return Json(payload);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Json(new { unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            await _notificationService.MarkAllAsReadAsync(userId.Value);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { unreadCount = 0 });
            }

            TempData["Message"] = "Toutes les notifications ont été marquées comme lues.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptInterview(int id)
        {
            return await RespondInterviewAsync(id, true);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineInterview(int id)
        {
            return await RespondInterviewAsync(id, false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId.Value);

            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);
            await _notificationService.BroadcastUnreadCountAsync(userId.Value, unreadCount);

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> RespondInterviewAsync(int id, bool accepted)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId.Value);

            if (notification == null) return NotFound();

            if (notification.Type != "InterviewProposal" || string.IsNullOrWhiteSpace(notification.Content))
            {
                return BadRequest();
            }

            var payload = JsonSerializer.Deserialize<InterviewProposalPayload>(notification.Content);
            if (payload == null)
            {
                return BadRequest();
            }

            notification.IsRead = true;

            await _notificationService.CreateAsync(
                payload.AlumniId,
                accepted ? "InterviewAccepted" : "InterviewDeclined",
                accepted
                    ? $"{payload.StudentName} a confirmé l'entretien pour l'offre '{payload.JobTitle}'. Créneau proposé: {payload.TimeSlot}."
                    : $"{payload.StudentName} a refusé l'entretien pour l'offre '{payload.JobTitle}'.",
                false,
                accepted ? $"Entretien accepté - {payload.JobTitle}" : $"Entretien refusé - {payload.JobTitle}");

            TempData["Message"] = accepted
                ? "Entretien confirmé. L'alumni a été notifié."
                : "Entretien refusé. L'alumni a été notifié.";

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

        private sealed class InterviewProposalPayload
        {
            public int ApplicationId { get; set; }
            public int JobId { get; set; }
            public string JobTitle { get; set; } = string.Empty;
            public int AlumniId { get; set; }
            public string AlumniName { get; set; } = string.Empty;
            public int StudentId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string TimeSlot { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }
}
