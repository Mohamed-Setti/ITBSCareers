using System.Security.Claims;
using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly CarriereDbContext _context;

        public AdminController(CarriereDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var admin = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (admin == null)
            {
                return RedirectToAction("Login", "User");
            }

            var hasAlumniRequestsTable = await IsAlumniRequestsTableAvailableAsync();
            var pendingRequests = hasAlumniRequestsTable
                ? await _context.AlumniRequests.CountAsync(r => r.Status == "Pending")
                : 0;

            var initialsParts = (admin.FullName ?? "A")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = string.Concat(initialsParts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
            if (string.IsNullOrWhiteSpace(initials))
            {
                initials = "A";
            }

            var vm = new AdminProfileViewModel
            {
                FullName = admin.FullName,
                Email = admin.Email,
                Initials = initials,
                Roles = admin.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).Distinct().ToList(),
                TotalUsers = await _context.Users.CountAsync(),
                StudentsCount = await _context.Users.CountAsync(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Student")),
                AlumniCount = await _context.Users.CountAsync(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Alumni")),
                AdminsCount = await _context.Users.CountAsync(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin")),
                TotalOffers = await _context.JobOffers.CountAsync(),
                TotalApplications = await _context.Applications.CountAsync(),
                PendingAlumniRequests = pendingRequests,
                AcceptedApplications = await _context.Applications.CountAsync(a => a.Status == "Accepted"),
                RejectedApplications = await _context.Applications.CountAsync(a => a.Status == "Rejected"),
                InterviewProposals = await _context.Notifications.CountAsync(n => n.Type == "InterviewProposal"),
                NotificationsLast14Days = await _context.Notifications.CountAsync(n => n.CreatedAt != null && n.CreatedAt >= DateTime.Now.AddDays(-14)),
                NewUsersLast14Days = await _context.Users.CountAsync(u => u.CreatedAt != null && u.CreatedAt >= DateTime.Now.AddDays(-14)),
                NewOffersLast14Days = await _context.JobOffers.CountAsync(o => o.CreatedAt != null && o.CreatedAt >= DateTime.Now.AddDays(-14)),
                PendingCandidaturesCount = await _context.Applications.CountAsync(a => a.Status == "Pending"),
                ValidatedAlumniCount = await _context.Users.CountAsync(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Alumni") && u.Alumni != null),
                ActiveUsersCount = await _context.Users.CountAsync(u => u.CreatedAt != null && u.CreatedAt >= DateTime.Now.AddDays(-30))
            };

            ViewBag.HasAlumniRequestsTable = hasAlumniRequestsTable;
            return View(vm);
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                ViewBag.Warning = "La table AlumniRequests est absente. Crée-la pour valider les demandes alumni.";
                return View(new List<AlumniRequest>());
            }

            var pendingRequests = await _context.AlumniRequests
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return View(pendingRequests);
        }

        public async Task<IActionResult> Users(string? query, string? roleFilter)
        {
            var roles = await _context.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToListAsync();

            var usersQuery = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Student)
                .Include(u => u.Alumni)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                usersQuery = usersQuery.Where(u => u.FullName.Contains(q) || u.Email.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                usersQuery = usersQuery.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == roleFilter));
            }

            var users = await usersQuery
                .OrderBy(u => u.FullName)
                .Select(u => new AdminUserItemViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    Roles = u.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).Distinct().ToList(),
                    IsAdmin = u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin"),
                    IsAlumni = u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Alumni"),
                    IsStudent = u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Student")
                })
                .ToListAsync();

            return View(new AdminUserManagementViewModel
            {
                Query = query,
                RoleFilter = roleFilter,
                Roles = roles,
                Users = users
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int userId, string roleName)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            roleName = roleName?.Trim() ?? string.Empty;
            var validRoles = new[] { "Student", "Alumni", "Admin" };
            if (!validRoles.Contains(roleName))
            {
                TempData["AdminError"] = "Rôle invalide.";
                return RedirectToAction(nameof(Users));
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                role = new Role { Name = roleName };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            _context.UserRoles.RemoveRange(user.UserRoles);
            _context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] = $"Le rôle de {user.FullName} a été mis à jour vers {roleName}.";
            return RedirectToAction(nameof(Users));
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
            if (int.TryParse(claimValue, out var userId))
            {
                return userId;
            }

            return HttpContext.Session.GetInt32("UserId");
        }
    }
}
