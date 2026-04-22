using System.Security.Claims;
using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize]
    public class JobOfferController : Controller
    {
        private readonly CarriereDbContext _context;

        public JobOfferController(CarriereDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "VerifiedAlumni")]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var offers = await _context.JobOffers
                .Where(o => o.AlumniId == userId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(offers);
        }

        [Authorize(Policy = "VerifiedAlumni")]
        public IActionResult Create()
        {
            return View(new JobOffer { Type = "Stage" });
        }

        [Authorize(Policy = "VerifiedAlumni")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobOffer offer)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            ModelState.Remove(nameof(JobOffer.Alumni));
            ModelState.Remove(nameof(JobOffer.Applications));

            if (string.IsNullOrWhiteSpace(offer.Title))
            {
                ModelState.AddModelError(nameof(JobOffer.Title), "Title is required.");
            }

            if (string.IsNullOrWhiteSpace(offer.Type) || (offer.Type != "Stage" && offer.Type != "Emploi"))
            {
                ModelState.AddModelError(nameof(JobOffer.Type), "Type must be Stage or Emploi.");
            }

            if (!ModelState.IsValid)
            {
                return View(offer);
            }

            offer.Title = offer.Title.Trim();
            offer.Description = offer.Description?.Trim();
            offer.Location = offer.Location?.Trim();
            offer.AlumniId = userId.Value;
            offer.CreatedAt = DateTime.Now;

            _context.JobOffers.Add(offer);
            await _context.SaveChangesAsync();

            TempData["JobOfferSuccess"] = "Offre publiée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Student,Alumni,Admin")]
        public async Task<IActionResult> Feed(string? type, string? title, string? location, string? publisher)
        {
            var vm = new JobOfferFeedViewModel
            {
                Type = type,
                Title = title,
                Location = location,
                Publisher = publisher
            };

            var query = _context.JobOffers
                .Include(o => o.Alumni)
                .Include(o => o.Applications)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(o => o.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                var t = title.Trim();
                query = query.Where(o => o.Title.Contains(t));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var l = location.Trim();
                query = query.Where(o => o.Location != null && o.Location.Contains(l));
            }

            if (!string.IsNullOrWhiteSpace(publisher))
            {
                var p = publisher.Trim();
                query = query.Where(o => o.Alumni.FullName.Contains(p));
            }

            vm.Results = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new JobOfferFeedItemViewModel
                {
                    JobId = o.JobId,
                    OfferTitle = o.Title,
                    Description = o.Description,
                    Type = o.Type,
                    Location = o.Location,
                    CreatedAt = o.CreatedAt,
                    AlumniId = o.AlumniId,
                    PublisherName = o.Alumni.FullName,
                    PublisherEmail = o.Alumni.Email,
                    ApplicationsCount = o.Applications.Count
                })
                .ToListAsync();

            return View(vm);
        }

        [Authorize(Roles = "Student,Alumni,Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var offer = await _context.JobOffers
                .Include(o => o.Alumni)
                .Include(o => o.Applications)
                .FirstOrDefaultAsync(o => o.JobId == id);

            if (offer == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            var isStudent = User.IsInRole("Student");
            var isOwner = currentUserId.HasValue && offer.AlumniId == currentUserId.Value;
            var alreadyApplied = currentUserId.HasValue && offer.Applications.Any(a => a.StudentId == currentUserId.Value);

            ViewBag.IsStudent = isStudent;
            ViewBag.IsOwner = isOwner;
            ViewBag.AlreadyApplied = alreadyApplied;

            return View(offer);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var offer = await _context.JobOffers.FirstOrDefaultAsync(o => o.JobId == id);
            if (offer == null)
            {
                return NotFound();
            }

            if (offer.AlumniId == userId.Value)
            {
                TempData["JobOfferError"] = "Vous ne pouvez pas postuler à votre propre offre.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var alreadyApplied = await _context.Applications
                .AnyAsync(a => a.JobId == id && a.StudentId == userId.Value);

            if (alreadyApplied)
            {
                TempData["JobOfferError"] = "Vous avez déjà postulé à cette offre.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var latestCv = await _context.Cvs
                .Where(c => c.UserId == userId.Value)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync();

            var application = new Application
            {
                JobId = id,
                StudentId = userId.Value,
                Cvid = latestCv?.Cvid,
                Status = "Pending",
                AppliedAt = DateTime.Now
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            TempData["JobOfferSuccess"] = "Postulation envoyée avec succès.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = "VerifiedAlumni")]
        public async Task<IActionResult> Applications(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var offer = await _context.JobOffers
                .FirstOrDefaultAsync(o => o.JobId == id && o.AlumniId == userId.Value);

            if (offer == null)
            {
                return Forbid();
            }

            ViewBag.OfferTitle = offer.Title;
            ViewBag.OfferId = offer.JobId;

            var applications = await _context.Applications
                .Where(a => a.JobId == id)
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
                .Include(a => a.Cv)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(applications);
        }

        [Authorize(Policy = "VerifiedAlumni")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptApplication(int applicationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var application = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Alumni)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Job.AlumniId != userId.Value)
            {
                return Forbid();
            }

            application.Status = "Accepted";

            _context.Notifications.Add(new Notification
            {
                UserId = application.StudentId,
                Type = "Application",
                Content = $"Votre candidature à l'offre '{application.Job.Title}' a été acceptée par {application.Job.Alumni.FullName}.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["JobOfferSuccess"] = "La candidature a été acceptée.";
            return RedirectToAction(nameof(Applications), new { id = application.JobId });
        }

        [Authorize(Policy = "VerifiedAlumni")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int applicationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var application = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Alumni)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Job.AlumniId != userId.Value)
            {
                return Forbid();
            }

            application.Status = "Rejected";

            _context.Notifications.Add(new Notification
            {
                UserId = application.StudentId,
                Type = "Application",
                Content = $"Votre candidature à l'offre '{application.Job.Title}' a été refusée par {application.Job.Alumni.FullName}.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["JobOfferSuccess"] = "La candidature a été refusée.";
            return RedirectToAction(nameof(Applications), new { id = application.JobId });
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
