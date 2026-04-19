using System.Security.Claims;
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
                TempData["Message"] = "You already have a pending alumni request.";
                return RedirectToAction("Profile", "User");
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
                TempData["Message"] = "You already have a pending alumni request.";
                return RedirectToAction("Profile", "User");
            }

            request.UserId = userId.Value;
            request.Status = "Pending";
            request.CreatedAt = DateTime.Now;
            request.ReviewedAt = null;
            request.ReviewedBy = null;

            _context.AlumniRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Your alumni request has been submitted.";
            return RedirectToAction("Profile", "User");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                TempData["Message"] = "Alumni request feature is not available yet. Please create table 'AlumniRequests'.";
                return RedirectToAction("Index", "Dashboard");
            }

            var pendingRequests = await _context.AlumniRequests
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return View(pendingRequests);
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
                return RedirectToAction(nameof(Pending));
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

            return RedirectToAction(nameof(Pending));
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
