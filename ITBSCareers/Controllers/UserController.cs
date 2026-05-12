using System.Security.Claims;
using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
using ITBSCareers.Models.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    public class UserController : Controller
    {
        private readonly CarriereDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public UserController(CarriereDbContext context)
        {
            _context = context;
        }

        // GET: UserController
        public ActionResult Index()
        {
            return RedirectToAction("Login", "User");
        }

        // GET: UserController/Details/5
        public ActionResult Details(int id)
        {
            var user = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.UserInterests)
                    .ThenInclude(ui => ui.Interest)
                .Include(u => u.Alumni)
                .Include(u => u.JobOffers)
                .FirstOrDefault(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [AllowAnonymous]
        // GET: UserController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserController/Create
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(ITBSCareers.Models.Carriere.User.Email), "This email is already used.");
                return View(user);
            }

            var plainPassword = user.PasswordHash;
            user.CreatedAt = DateTime.Now;
            user.PasswordHash = _passwordHasher.HashPassword(user, plainPassword);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await AssignSingleRoleAsync(user.UserId, "Student");
            await SignInUserAsync(user.UserId);

            return RedirectToAction("Index", "Dashboard");
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View("LogIn");
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            email = (email ?? string.Empty).Trim();
            password = (password ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Invalid email or password";
                return View("LogIn");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password";
                return View("LogIn");
            }

            PasswordVerificationResult verificationResult;

            try
            {
                verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            catch (FormatException)
            {
                verificationResult = PasswordVerificationResult.Failed;
            }

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                if (user.PasswordHash != password)
                {
                    ViewBag.Error = "Invalid email or password";
                    return View("LogIn");
                }

                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await _context.SaveChangesAsync();
            }
            else if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await _context.SaveChangesAsync();
            }

            await SignInUserAsync(user.UserId);
            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "User");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: UserController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UserController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserController/SelectSkillsInterests/5
        [Authorize]
        public ActionResult SelectSkillsInterests(int id, string? returnUrl = null)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (currentUserId != id)
            {
                return Forbid();
            }

            var user = _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.UserInterests)
                    .ThenInclude(ui => ui.Interest)
                .FirstOrDefault(u => u.UserId == id);

            if (user == null) return NotFound();

            var userSkillIds = user.UserSkills.Select(us => us.SkillId).ToList();
            var userInterestIds = user.UserInterests.Select(ui => ui.InterestId).ToList();

            ViewBag.ReturnUrl = returnUrl;

            var vm = new SelectSkillsInterestsViewModel
            {
                UserId = id,
                Skills = _context.Skills.Select(s => new CheckboxItem
                {
                    Id = s.SkillId,
                    Name = s.Name,
                    IsSelected = userSkillIds.Contains(s.SkillId)
                }).ToList(),
                Interests = _context.Interests.Select(i => new CheckboxItem
                {
                    Id = i.InterestId,
                    Name = i.Name,
                    IsSelected = userInterestIds.Contains(i.InterestId)
                }).ToList()
            };

            return View(vm);
        }

        // POST: UserController/SelectSkillsInterests
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectSkillsInterests(SelectSkillsInterestsViewModel vm, string? returnUrl)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (currentUserId != vm.UserId)
            {
                return Forbid();
            }

            var user = _context.Users.FirstOrDefault(u => u.UserId == vm.UserId);
            if (user == null) return NotFound();

            var selectedSkillIds = Request.Form["SkillIds"]
                .Where(x => int.TryParse(x, out _))
                .Select(int.Parse)
                .Distinct()
                .ToList();

            var selectedInterestIds = Request.Form["InterestIds"]
                .Where(x => int.TryParse(x, out _))
                .Select(int.Parse)
                .Distinct()
                .ToList();

            var existingSkills = _context.UserSkills.Where(us => us.UserId == user.UserId).ToList();
            _context.UserSkills.RemoveRange(existingSkills);
            foreach (var skillId in selectedSkillIds)
                _context.UserSkills.Add(new UserSkill { UserId = user.UserId, SkillId = skillId });

            var existingInterests = _context.UserInterests.Where(ui => ui.UserId == user.UserId).ToList();
            _context.UserInterests.RemoveRange(existingInterests);
            foreach (var interestId in selectedInterestIds)
                _context.UserInterests.Add(new UserInterest { UserId = user.UserId, InterestId = interestId });

            _context.SaveChanges();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.UserInterests)
                    .ThenInclude(ui => ui.Interest)
                .Include(u => u.Experiences)
                .Include(u => u.Cvs)
                .Include(u => u.Applications)
                    .ThenInclude(a => a.Job)
                        .ThenInclude(j => j.Alumni)
                .Include(u => u.JobOffers)
                    .ThenInclude(j => j.Applications)
                .Include(u => u.Alumni)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null) return NotFound();

            if (user.Alumni != null)
            {
                ViewBag.PendingContactRequests = await _context.Set<MentorshipRequest>()
                    .Where(r => r.AlumniId == user.UserId && r.Status == "Pending")
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ContactRequestViewModel
                    {
                        RequestId = r.MentorshipRequestId,
                        StudentId = r.StudentId,
                        StudentName = r.Student.FullName,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();
            }

            if (await IsAlumniRequestsTableAvailableAsync())
            {
                ViewBag.AlumniRequests = await (from request in _context.AlumniRequests.AsNoTracking()
                                                join reviewer in _context.Users.AsNoTracking() on request.ReviewedBy equals reviewer.UserId into reviewerGroup
                                                from reviewer in reviewerGroup.DefaultIfEmpty()
                                                where request.UserId == user.UserId
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
                                                }).ToListAsync();
            }

            return View(user);
        }

        [Authorize(Roles = "Alumni")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContactVisibility(bool isContactPublic)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var alumni = await _context.Alumnis.FirstOrDefaultAsync(a => a.AlumniId == userId.Value);
            if (alumni == null)
            {
                return NotFound();
            }

            alumni.IsContactPublic = isContactPublic;
            await _context.SaveChangesAsync();

            TempData["Message"] = isContactPublic
                ? "Votre profil de contact est désormais public."
                : "Votre profil de contact est désormais privé.";

            return RedirectToAction(nameof(Profile));
        }

        [Authorize(Roles = "Student")]
        public IActionResult RequestInterview(int applicationId)
        {
            TempData["Message"] = "Les entretiens sont maintenant proposés par l'alumni. Consultez vos notifications.";
            return RedirectToAction("Index", "Notification");
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestInterview(int applicationId, string subject, string message)
        {
            TempData["Message"] = "Les entretiens sont maintenant proposés par l'alumni. Consultez vos notifications.";
            return RedirectToAction("Index", "Notification");
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

        private async Task AssignSingleRoleAsync(int userId, string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                role = new Role { Name = roleName };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            var existingRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            if (existingRoles.Count > 0)
            {
                _context.UserRoles.RemoveRange(existingRoles);
                await _context.SaveChangesAsync();
            }

            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = role.RoleId
            });

            await _context.SaveChangesAsync();
        }

        private async Task SignInUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email)
            };

            foreach (var roleName in user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).Distinct())
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            HttpContext.Session.SetInt32("UserId", user.UserId);
        }
    }
}
