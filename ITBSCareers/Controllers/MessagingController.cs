using System.Security.Claims;
using ITBSCareers.Models.Messaging;
using ITBSCareers.Hubs;
using ITBSCareers.Services.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ITBSCareers.Controllers;

[Authorize]
public class MessagingController : Controller
{
    private const long MaxAttachmentSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip"
    };

    private readonly IPrivateMessagingService _service;
    private readonly IHubContext<MessagingHub> _hubContext;
    private readonly IWebHostEnvironment _environment;

    public MessagingController(IPrivateMessagingService service, IHubContext<MessagingHub> hubContext, IWebHostEnvironment environment)
    {
        _service = service;
        _hubContext = hubContext;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? search, string? contactsSearch, int? conversationId)
    {
        var userId = RequireCurrentUserId();
        var vm = await _service.GetInboxAsync(userId, search, contactsSearch, conversationId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ConversationData(int id)
    {
        var userId = RequireCurrentUserId();
        var vm = await _service.GetConversationAsync(userId, id);
        if (vm == null)
        {
            return NotFound();
        }

        return Json(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? q, string? contactsQ)
    {
        var userId = RequireCurrentUserId();
        var vm = await _service.GetInboxAsync(userId, q, contactsQ);
        return Json(new
        {
            unreadCount = vm.UnreadCount,
            conversations = vm.Conversations.Select(c => new
            {
                c.ConversationId,
                c.OtherUserId,
                c.OtherUserName,
                c.Subject,
                c.LastMessage,
                lastMessageAt = c.LastMessageAt,
                c.UnreadCount,
                c.IsBlocked
            }),
            contacts = vm.EligibleContacts.Select(c => new
            {
                c.UserId,
                c.FullName,
                c.CompanyName,
                c.Position,
                c.RoleName,
                c.IsContactPublic,
                c.IsOnline,
                c.ConversationId,
                c.LastInteractionAt,
                c.IsBlocked,
                c.CanMessage,
                c.CanRequestContact,
                c.HasPendingContactRequest,
                c.ActionLabel
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(StartConversationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = RequireCurrentUserId();
        var result = await _service.StartConversationAsync(userId, dto.OtherUserId, dto.Subject);
        if (!result.success)
        {
            return BadRequest(new { error = result.error });
        }

        return Json(new { conversationId = result.conversationId, redirectUrl = Url.Action(nameof(Index), new { conversationId = result.conversationId }) });
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestContact(int alumniUserId)
    {
        var userId = RequireCurrentUserId();
        var result = await _service.RequestContactAsync(userId, alumniUserId);
        if (!result.success)
        {
            return BadRequest(new { error = result.error });
        }

        return Json(new { success = true, pending = result.pending });
    }

    [Authorize(Roles = "Alumni")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptContactRequest(int requestId)
    {
        var alumniId = RequireCurrentUserId();
        var result = await _service.RespondToContactRequestAsync(alumniId, requestId, true);
        if (!result.success)
        {
            TempData["Message"] = result.error;
            return RedirectToAction("Profile", "User");
        }

        TempData["Message"] = "Demande de contact acceptée.";
        return RedirectToAction("Profile", "User");
    }

    [Authorize(Roles = "Alumni")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectContactRequest(int requestId)
    {
        var alumniId = RequireCurrentUserId();
        var result = await _service.RespondToContactRequestAsync(alumniId, requestId, false);
        if (!result.success)
        {
            TempData["Message"] = result.error;
            return RedirectToAction("Profile", "User");
        }

        TempData["Message"] = "Demande de contact refusée.";
        return RedirectToAction("Profile", "User");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int conversationId, string? content, IFormFile? attachment)
    {
        var userId = RequireCurrentUserId();
        var conversation = await _service.GetConversationAsync(userId, conversationId);
        if (conversation == null)
        {
            return NotFound();
        }

        var otherUserId = conversation.OtherUserId;
        (string? path, string? name, string? contentType, long? size) attachmentInfo;

        try
        {
            attachmentInfo = await SaveAttachmentAsync(conversationId, attachment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var result = await _service.SendMessageAsync(
            userId,
            conversationId,
            content,
            attachmentInfo.path,
            attachmentInfo.name,
            attachmentInfo.contentType,
            attachmentInfo.size);

        if (!result.success || result.message == null)
        {
            return BadRequest(new { error = result.error });
        }

        var payload = new
        {
            messageId = result.message.MessageId,
            conversationId,
            senderId = result.message.SenderId,
            senderName = result.message.SenderName,
            content = result.message.Content,
            attachmentPath = result.message.AttachmentPath,
            attachmentName = result.message.AttachmentName,
            isRead = result.message.IsRead,
            sentAt = result.message.SentAt,
            isMine = result.message.IsMine
        };

        var senderGroup = MessagingHub.GetUserGroupName(userId);
        var receiverGroup = MessagingHub.GetUserGroupName(otherUserId);
        var conversationGroup = MessagingHub.GetConversationGroupName(conversationId);

        await Task.WhenAll(
            _hubContext.Clients.Group(conversationGroup).SendAsync("ReceiveMessage", payload),
            _hubContext.Clients.Group(senderGroup).SendAsync("ReceiveMessage", payload),
            _hubContext.Clients.Group(receiverGroup).SendAsync("ReceiveMessage", payload));

        await _hubContext.Clients.Group(receiverGroup)
            .SendAsync("UnreadCountUpdated", new { userId = otherUserId, unreadCount = result.unreadCount });

        var currentUnread = await _service.GetInboxAsync(userId, null, null, conversationId);
        await _hubContext.Clients.Group(senderGroup)
            .SendAsync("UnreadCountUpdated", new { userId, unreadCount = currentUnread.UnreadCount });

        return Json(new { success = true, message = payload, unreadCount = result.unreadCount });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(int conversationId)
    {
        var userId = RequireCurrentUserId();
        var ok = await _service.MarkReadAsync(userId, conversationId);
        if (!ok)
        {
            return NotFound();
        }

        var inbox = await _service.GetInboxAsync(userId, null, null, conversationId);
        await _hubContext.Clients.Group(MessagingHub.GetUserGroupName(userId))
            .SendAsync("UnreadCountUpdated", new { userId, unreadCount = inbox.UnreadCount });

        return Json(new { success = true, unreadCount = inbox.UnreadCount });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(int blockedUserId, string? reason)
    {
        var userId = RequireCurrentUserId();
        var ok = await _service.BlockUserAsync(userId, blockedUserId, reason);
        if (!ok)
        {
            return BadRequest(new { error = "Impossible de bloquer cet utilisateur." });
        }

        await _hubContext.Clients.Group(MessagingHub.GetUserGroupName(blockedUserId))
            .SendAsync("UserBlocked", new { blockedBy = userId });

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(int blockedUserId)
    {
        var userId = RequireCurrentUserId();
        var ok = await _service.UnblockUserAsync(userId, blockedUserId);
        if (!ok)
        {
            return BadRequest(new { error = "Impossible de débloquer cet utilisateur." });
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int reportedUserId, string reason)
    {
        var userId = RequireCurrentUserId();
        var ok = await _service.ReportUserAsync(userId, reportedUserId, reason);
        if (!ok)
        {
            return BadRequest(new { error = "Signalement impossible." });
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int conversationId)
    {
        var userId = RequireCurrentUserId();
        var ok = await _service.DeleteConversationAsync(userId, conversationId);
        if (!ok)
        {
            return NotFound();
        }

        return Json(new { success = true });
    }

    private int RequireCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claimValue, out var id))
        {
            return id;
        }

        var sessionUserId = HttpContext.Session.GetInt32("UserId");
        return sessionUserId ?? throw new UnauthorizedAccessException();
    }

    private async Task<(string? path, string? name, string? contentType, long? size)> SaveAttachmentAsync(int conversationId, IFormFile? attachment)
    {
        if (attachment == null || attachment.Length == 0)
        {
            return (null, null, null, null);
        }

        if (attachment.Length > MaxAttachmentSize)
        {
            throw new InvalidOperationException("La pièce jointe dépasse la taille maximale autorisée.");
        }

        var originalExtension = Path.GetExtension(attachment.FileName);
        if (string.IsNullOrWhiteSpace(originalExtension))
        {
            originalExtension = ".bin";
        }

        if (!AllowedAttachmentExtensions.Contains(originalExtension))
        {
            throw new InvalidOperationException("Type de pièce jointe non autorisé.");
        }

        var relativeFolder = Path.Combine("uploads", "messages", conversationId.ToString());
        var absoluteFolder = Path.Combine(_environment.WebRootPath, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"{Guid.NewGuid():N}{originalExtension}";
        var absolutePath = Path.Combine(absoluteFolder, fileName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await attachment.CopyToAsync(stream);
        }

        var relativePath = "/" + Path.Combine(relativeFolder, fileName).Replace(Path.DirectorySeparatorChar, '/');
        return (relativePath, attachment.FileName, attachment.ContentType, attachment.Length);
    }
}
