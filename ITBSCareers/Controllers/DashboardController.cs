using System.Security.Claims;
using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly CarriereDbContext _context;

        public DashboardController(CarriereDbContext context)
        {
            _context = context;
        }

        // GET: DashboardController
        public async Task<ActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Experiences)
                .Include(u => u.UserSkills)
                .Include(u => u.UserInterests)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            var roleNames = user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.Name)
                .Distinct()
                .ToList();

            var alumniRequestsAvailable = await IsAlumniRequestsTableAvailableAsync();
            string? latestRequestStatus = null;
            var hasApprovedRequest = false;
            var pendingAlumniRequestsCount = 0;

            if (alumniRequestsAvailable)
            {
                latestRequestStatus = await _context.AlumniRequests
                    .Where(r => r.UserId == userId.Value)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => r.Status)
                    .FirstOrDefaultAsync();

                hasApprovedRequest = await _context.AlumniRequests
                    .AnyAsync(r => r.UserId == userId.Value && r.Status == "Approved");

                if (roleNames.Contains("Admin"))
                {
                    pendingAlumniRequestsCount = await _context.AlumniRequests
                        .CountAsync(r => r.Status == "Pending");
                }
            }

            var isVerifiedAlumni = await _context.Alumnis.AnyAsync(a => a.AlumniId == userId.Value)
                                  && hasApprovedRequest
                                  && roleNames.Contains("Alumni");

            var vm = new DashboardViewModel
            {
                FullName = user.FullName,
                Roles = roleNames,
                ExperiencesCount = user.Experiences.Count,
                SkillsCount = user.UserSkills.Count,
                InterestsCount = user.UserInterests.Count,
                IsStudent = roleNames.Contains("Student"),
                IsAdmin = roleNames.Contains("Admin"),
                IsVerifiedAlumni = isVerifiedAlumni,
                AlumniRequestStatus = latestRequestStatus,
                PendingAlumniRequestsCount = pendingAlumniRequestsCount
            };

            if (!alumniRequestsAvailable)
            {
                ViewBag.Warning = "Alumni requests module is disabled because table 'AlumniRequests' is missing.";
            }

            return View(vm);
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
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var userId))
            {
                return userId;
            }

            return HttpContext.Session.GetInt32("UserId");
        }
    }
}
