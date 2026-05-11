using System.Security.Claims;
using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize]
    public class AlumniRequestController : Controller
    {
        private readonly CarriereDbContext _context;

        public AlumniRequestController(CarriereDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create()
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                TempData["Message"] = "Alumni request feature is not available yet. Please create table 'AlumniRequests'.";
                return RedirectToAction("Index", "Dashboard");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var hasPendingRequest = await _context.AlumniRequests
                .AnyAsync(r => r.UserId == userId.Value && r.Status == "Pending");

            if (hasPendingRequest)
            {
                TempData["Message"] = "Vous avez déjà une demande alumni en attente.";
                return RedirectToAction(nameof(MyRequests));
            }

            return View(new AlumniRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create(AlumniRequest request)
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                TempData["Message"] = "Alumni request feature is not available yet. Please create table 'AlumniRequests'.";
                return RedirectToAction("Index", "Dashboard");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var hasPendingRequest = await _context.AlumniRequests
                .AnyAsync(r => r.UserId == userId.Value && r.Status == "Pending");

            if (hasPendingRequest)
            {
                TempData["Message"] = "Vous avez déjà une demande alumni en attente.";
                return RedirectToAction(nameof(MyRequests));
            }

            request.UserId = userId.Value;
            request.Status = "Pending";
            request.CreatedAt = DateTime.Now;
            request.ReviewedAt = null;
            request.ReviewedBy = null;

            _context.AlumniRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Votre demande alumni a bien été envoyée.";
            return RedirectToAction(nameof(MyRequests));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage(string? filter = "All")
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                TempData["Message"] = "La fonctionnalité des demandes alumni n'est pas disponible. Créez la table 'AlumniRequests'.";
                return RedirectToAction("Index", "Dashboard");
            }

            var requests = await GetAlumniRequestItemsAsync();
            var currentFilter = string.IsNullOrWhiteSpace(filter) ? "All" : filter.Trim();

            ViewBag.Filter = currentFilter;
            ViewBag.TotalCount = requests.Count;
            ViewBag.PendingCount = requests.Count(r => r.Status == "Pending");
            ViewBag.ApprovedCount = requests.Count(r => r.Status == "Approved");
            ViewBag.RejectedCount = requests.Count(r => r.Status == "Rejected");

            requests = currentFilter.ToLowerInvariant() switch
            {
                "pending" => requests.Where(r => r.Status == "Pending").ToList(),
                "approved" => requests.Where(r => r.Status == "Approved").ToList(),
                "rejected" => requests.Where(r => r.Status == "Rejected").ToList(),
                _ => requests
            };

            return View(requests);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            return RedirectToAction(nameof(Manage), new { filter = "Pending" });
        }

        [Authorize]
        public async Task<IActionResult> MyRequests()
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                TempData["Message"] = "La fonctionnalité des demandes alumni n'est pas disponible. Créez la table 'AlumniRequests'.";
                return RedirectToAction("Profile", "User");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var requests = await GetAlumniRequestItemsAsync(userId.Value);
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                TempData["Message"] = "Alumni request feature is not available yet. Please create table 'AlumniRequests'.";
                return RedirectToAction("Index", "Dashboard");
            }

            var adminId = GetCurrentUserId();
            if (adminId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var request = await _context.AlumniRequests.FirstOrDefaultAsync(r => r.AlumniRequestId == id);
            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
            return RedirectToAction(nameof(Manage));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var alumni = await _context.Alumnis.FirstOrDefaultAsync(a => a.AlumniId == request.UserId);
                if (alumni == null)
                {
                    alumni = new Alumni
                    {
                        AlumniId = request.UserId,
                        CompanyName = request.CompanyName,
                        Position = request.Position
                    };
                    _context.Alumnis.Add(alumni);
                }
                else
                {
                    alumni.CompanyName = request.CompanyName;
                    alumni.Position = request.Position;
                }

                var alumniRole = await GetOrCreateRoleAsync("Alumni");
                var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");

                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == request.UserId)
                    .ToListAsync();

                var alreadyAlumni = userRoles.Any(ur => ur.RoleId == alumniRole.RoleId);
                if (!alreadyAlumni)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = request.UserId,
                        RoleId = alumniRole.RoleId
                    });
                }

                if (studentRole != null)
                {
                    var studentLinks = userRoles.Where(ur => ur.RoleId == studentRole.RoleId).ToList();
                    if (studentLinks.Count > 0)
                    {
                        _context.UserRoles.RemoveRange(studentLinks);
                    }
                }

                request.Status = "Approved";
                request.ReviewedBy = adminId.Value;
                request.ReviewedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return RedirectToAction(nameof(Manage));
        }

        private async Task<List<AlumniRequestListItemViewModel>> GetAlumniRequestItemsAsync(int? userId = null)
        {
            var requestsQuery = _context.AlumniRequests.AsNoTracking();

            if (userId.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.UserId == userId.Value);
            }

            var query = from request in requestsQuery
                        join user in _context.Users.AsNoTracking() on request.UserId equals user.UserId
                        join reviewer in _context.Users.AsNoTracking() on request.ReviewedBy equals reviewer.UserId into reviewerGroup
                        from reviewer in reviewerGroup.DefaultIfEmpty()
                        orderby request.CreatedAt descending
                        select new AlumniRequestListItemViewModel
                        {
                            AlumniRequestId = request.AlumniRequestId,
                            UserId = request.UserId,
                            UserName = user.FullName,
                            CompanyName = request.CompanyName,
                            Position = request.Position,
                            ProofFilePath = request.ProofFilePath,
                            Status = request.Status,
                            ReviewedByName = reviewer != null ? reviewer.FullName : null,
                            ReviewedAt = request.ReviewedAt,
                            CreatedAt = request.CreatedAt
                        };

            return await query.ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                TempData["Message"] = "Alumni request feature is not available yet. Please create table 'AlumniRequests'.";
                return RedirectToAction("Index", "Dashboard");
            }

            var adminId = GetCurrentUserId();
            if (adminId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var request = await _context.AlumniRequests.FirstOrDefaultAsync(r => r.AlumniRequestId == id);
            if (request == null)
            {
                return NotFound();
            }

            if (request.Status == "Pending")
            {
                request.Status = "Rejected";
                request.ReviewedBy = adminId.Value;
                request.ReviewedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Pending));
        }

        private async Task<bool> IsAlumniRequestsTableAvailableAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT TOP (1) 1 FROM [dbo].[AlumniRequests]");
                return true;
            }
            catch (SqlException ex) when (ex.Message.Contains("Invalid object name 'AlumniRequests'"))
            {
                return false;
            }
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

        private async Task<Role> GetOrCreateRoleAsync(string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role != null)
            {
                return role;
            }

            role = new Role { Name = roleName };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }
    }
}
