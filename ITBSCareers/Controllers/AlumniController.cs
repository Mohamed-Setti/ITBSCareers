using System.Security.Claims;
using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize(Policy = "VerifiedAlumni")]
    public class AlumniController : Controller
    {
        private readonly CarriereDbContext _context;

        public AlumniController(CarriereDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? query, string? degree, string? level, string? skill, string? interest)
        {
            var vm = new AlumniCvThequeViewModel
            {
                Query = query,
                Degree = degree,
                Level = level,
                Skill = skill,
                Interest = interest
            };

            vm.Degrees = await _context.Degrees
                .OrderBy(d => d.Name)
                .Select(d => d.Name)
                .ToListAsync();

            vm.Levels = await _context.Students
                .Where(s => s.Level != null)
                .Select(s => s.Level!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            vm.Skills = await _context.Skills
                .OrderBy(s => s.Name)
                .Select(s => s.Name)
                .ToListAsync();

            vm.Interests = await _context.Interests
                .OrderBy(i => i.Name)
                .Select(i => i.Name)
                .ToListAsync();

            var cvQuery = _context.Cvs
                .Include(c => c.User)
                    .ThenInclude(u => u.Student)
                        .ThenInclude(s => s.Degree)
                .Include(c => c.User)
                    .ThenInclude(u => u.UserSkills)
                        .ThenInclude(us => us.Skill)
                .Include(c => c.User)
                    .ThenInclude(u => u.UserInterests)
                        .ThenInclude(ui => ui.Interest)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                cvQuery = cvQuery.Where(c =>
                    c.User.FullName.Contains(q) ||
                    c.User.Email.Contains(q) ||
                    (c.User.Student != null && c.User.Student.Field != null && c.User.Student.Field.Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(degree))
            {
                cvQuery = cvQuery.Where(c => c.User.Student != null && c.User.Student.Degree.Name == degree);
            }

            if (!string.IsNullOrWhiteSpace(level))
            {
                cvQuery = cvQuery.Where(c => c.User.Student != null && c.User.Student.Level == level);
            }

            if (!string.IsNullOrWhiteSpace(skill))
            {
                cvQuery = cvQuery.Where(c => c.User.UserSkills.Any(us => us.Skill.Name == skill));
            }

            if (!string.IsNullOrWhiteSpace(interest))
            {
                cvQuery = cvQuery.Where(c => c.User.UserInterests.Any(ui => ui.Interest.Name == interest));
            }

            vm.Results = await cvQuery
                .OrderByDescending(c => c.UploadedAt)
                .Select(c => new AlumniCvItemViewModel
                {
                    Cvid = c.Cvid,
                    UserId = c.UserId,
                    FullName = c.User.FullName,
                    Email = c.User.Email,
                    Degree = c.User.Student != null ? c.User.Student.Degree.Name : null,
                    Field = c.User.Student != null ? c.User.Student.Field : null,
                    Level = c.User.Student != null ? c.User.Student.Level : null,
                    CvFilePath = c.FilePath,
                    CvUploadedAt = c.UploadedAt,
                    Skills = c.User.UserSkills.Select(us => us.Skill.Name).ToList(),
                    Interests = c.User.UserInterests.Select(ui => ui.Interest.Name).ToList()
                })
                .ToListAsync();

            return View(vm);
        }

        public async Task<IActionResult> Applications(string? filter = "All", string? query = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var currentFilter = string.IsNullOrWhiteSpace(filter) ? "All" : filter.Trim();

            IQueryable<Application> applicationsQuery = _context.Applications
                .AsNoTracking()
                .Include(a => a.Job)
                .Include(a => a.Student)
                    .ThenInclude(s => s.Student)
                        .ThenInclude(st => st.Degree)
                .Include(a => a.Student)
                    .ThenInclude(s => s.UserSkills)
                        .ThenInclude(us => us.Skill)
                .Include(a => a.Student)
                    .ThenInclude(s => s.UserInterests)
                        .ThenInclude(ui => ui.Interest)
                .Include(a => a.Student)
                    .ThenInclude(s => s.Experiences)
                .Include(a => a.Cv);

            applicationsQuery = applicationsQuery.Where(a => a.Job.AlumniId == userId.Value);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                applicationsQuery = applicationsQuery.Where(a =>
                    a.Student.FullName.Contains(term) ||
                    a.Student.Email.Contains(term) ||
                    a.Job.Title.Contains(term) ||
                    (a.Student.Student != null && a.Student.Student.Field != null && a.Student.Student.Field.Contains(term)));
            }

            var applications = await applicationsQuery
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var items = applications.Select(a => new AlumniApplicationItemViewModel
            {
                ApplicationId = a.ApplicationId,
                JobId = a.JobId,
                OfferTitle = a.Job.Title,
                OfferType = a.Job.Type,
                OfferLocation = a.Job.Location,
                StudentId = a.StudentId,
                StudentName = a.Student.FullName,
                StudentEmail = a.Student.Email,
                Degree = a.Student.Student?.Degree?.Name,
                Field = a.Student.Student?.Field,
                Level = a.Student.Student?.Level,
                CvFilePath = a.Cv?.FilePath,
                CvUploadedAt = a.Cv?.UploadedAt,
                Status = a.Status ?? "Pending",
                AppliedAt = a.AppliedAt,
                SkillsCount = a.Student.UserSkills.Count,
                InterestsCount = a.Student.UserInterests.Count,
                ExperiencesCount = a.Student.Experiences.Count,
                CanProposeInterview = string.Equals(a.Status ?? "Pending", "Accepted", StringComparison.OrdinalIgnoreCase)
            }).ToList();

            var model = new AlumniApplicationsBoardViewModel
            {
                Query = query,
                Filter = currentFilter,
                TotalCount = items.Count,
                PendingCount = items.Count(x => x.Status == "Pending"),
                AcceptedCount = items.Count(x => x.Status == "Accepted"),
                RejectedCount = items.Count(x => x.Status == "Rejected"),
                InterviewReadyCount = items.Count(x => x.CanProposeInterview),
                Applications = currentFilter.ToLowerInvariant() switch
                {
                    "pending" => items.Where(x => x.Status == "Pending").ToList(),
                    "accepted" => items.Where(x => x.Status == "Accepted").ToList(),
                    "rejected" => items.Where(x => x.Status == "Rejected").ToList(),
                    _ => items
                }
            };

            return View(model);
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
}
