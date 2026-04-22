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
    }
}
