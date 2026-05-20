using System.Security.Claims;
using System.Text.Json;
using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
using ITBSCareers.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize]
    public class JobOfferController : Controller
    {
        private readonly CarriereDbContext _context;
        private readonly INotificationService _notificationService;

        public JobOfferController(CarriereDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
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
            PopulateOfferLookups();
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

            offer.Title = offer.Title?.Trim();
            offer.Description = offer.Description?.Trim();
            offer.Location = offer.Location?.Trim();
            offer.RequiredDegree = offer.RequiredDegree?.Trim();
            offer.RequiredLevel = offer.RequiredLevel?.Trim();
            offer.RequiredField = offer.RequiredField?.Trim();

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
                PopulateOfferLookups(offer);
                return View(offer);
            }

            offer.AlumniId = userId.Value;
            offer.CreatedAt = DateTime.Now;
            offer.RequiredSkillsCsv = JoinSelectedIds(Request.Form["RequiredSkillIds"]);
            offer.RequiredInterestsCsv = JoinSelectedIds(Request.Form["RequiredInterestIds"]);

            _context.JobOffers.Add(offer);
            await _context.SaveChangesAsync();

            TempData["JobOfferSuccess"] = "Offre publiée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "VerifiedAlumni")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var offer = await _context.JobOffers
                .Include(o => o.Applications)
                .FirstOrDefaultAsync(o => o.JobId == id && o.AlumniId == userId.Value);

            if (offer == null)
            {
                return NotFound();
            }

            if (offer.Applications.Any())
            {
                _context.Applications.RemoveRange(offer.Applications);
            }

            _context.JobOffers.Remove(offer);
            await _context.SaveChangesAsync();

            TempData["JobOfferSuccess"] = "Offre supprimée.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Student,Alumni,Admin")]
        public async Task<IActionResult> Feed(string? type, string? title, string? location, string? publisher)
        {
            var userId = GetCurrentUserId();
            var isStudent = User.IsInRole("Student");

            var vm = new JobOfferFeedViewModel
            {
                Type = type,
                Title = title,
                Location = location,
                Publisher = publisher
            };

            var offersQuery = _context.JobOffers
                .Include(o => o.Alumni)
                .Include(o => o.Applications)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
            {
                offersQuery = offersQuery.Where(o => o.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                var t = title.Trim();
                offersQuery = offersQuery.Where(o => o.Title.Contains(t));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var l = location.Trim();
                offersQuery = offersQuery.Where(o => o.Location != null && o.Location.Contains(l));
            }

            if (!string.IsNullOrWhiteSpace(publisher))
            {
                var p = publisher.Trim();
                offersQuery = offersQuery.Where(o => o.Alumni.FullName.Contains(p));
            }

            var offers = await offersQuery.OrderByDescending(o => o.CreatedAt).ToListAsync();

            StudentProfileSnapshot? studentProfile = null;
            if (isStudent && userId != null)
            {
                studentProfile = await BuildStudentProfileSnapshotAsync(userId.Value);
            }

            var mapped = offers
                .Select(o => BuildFeedItem(o, studentProfile))
                .ToList();

            vm.Suggestions = isStudent
                ? mapped.Where(x => x.MatchScore > 0).OrderByDescending(x => x.MatchScore).ThenByDescending(x => x.CreatedAt).ToList()
                : new List<JobOfferFeedItemViewModel>();

            vm.Results = isStudent
                ? mapped.Where(x => x.MatchScore <= 0).OrderByDescending(x => x.CreatedAt).ToList()
                : mapped.OrderByDescending(x => x.CreatedAt).ToList();

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

            var skillIds = ParseCsvIds(offer.RequiredSkillsCsv);
            var interestIds = ParseCsvIds(offer.RequiredInterestsCsv);

            var skillNames = skillIds.Count > 0
                ? await _context.Skills
                    .Where(s => skillIds.Contains(s.SkillId))
                    .OrderBy(s => s.Name)
                    .Select(s => s.Name)
                    .ToListAsync()
                : new List<string>();

            var interestNames = interestIds.Count > 0
                ? await _context.Interests
                    .Where(i => interestIds.Contains(i.InterestId))
                    .OrderBy(i => i.Name)
                    .Select(i => i.Name)
                    .ToListAsync()
                : new List<string>();

            ViewBag.IsStudent = isStudent;
            ViewBag.IsOwner = isOwner;
            ViewBag.AlreadyApplied = alreadyApplied;
            ViewBag.RequiredSkills = skillNames;
            ViewBag.RequiredInterests = interestNames;

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

            var alreadyApplied = await _context.Applications.AnyAsync(a => a.JobId == id && a.StudentId == userId.Value);
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

            var offer = await _context.JobOffers.FirstOrDefaultAsync(o => o.JobId == id && o.AlumniId == userId.Value);
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

            if (application == null) return NotFound();
            if (application.Job.AlumniId != userId.Value) return Forbid();

            application.Status = "Accepted";

            await _notificationService.CreateAsync(
                application.StudentId,
                "Application",
                $"Votre candidature à l'offre '{application.Job.Title}' a été acceptée par {application.Job.Alumni.FullName}.",
                false,
                $"Candidature acceptée - {application.Job.Title}");

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

            if (application == null) return NotFound();
            if (application.Job.AlumniId != userId.Value) return Forbid();

            application.Status = "Rejected";

            await _notificationService.CreateAsync(
                application.StudentId,
                "Application",
                $"Votre candidature à l'offre '{application.Job.Title}' a été refusée par {application.Job.Alumni.FullName}.",
                false,
                $"Candidature refusée - {application.Job.Title}");

            await _context.SaveChangesAsync();

            TempData["JobOfferSuccess"] = "La candidature a été refusée.";
            return RedirectToAction(nameof(Applications), new { id = application.JobId });
        }

        [Authorize(Policy = "VerifiedAlumni")]
        public async Task<IActionResult> ProposeInterview(int applicationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var application = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Alumni)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null) return NotFound();
            if (application.Job.AlumniId != userId.Value) return Forbid();
            if ((application.Status ?? "Pending") != "Accepted")
            {
                TempData["JobOfferError"] = "L'entretien ne peut être proposé que pour une candidature acceptée.";
                return RedirectToAction(nameof(Applications), new { id = application.JobId });
            }

            ViewBag.Application = application;
            return View();
        }

        [Authorize(Policy = "VerifiedAlumni")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProposeInterview(int applicationId, string subject, string timeSlot, string message)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var application = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Alumni)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null) return NotFound();
            if (application.Job.AlumniId != userId.Value) return Forbid();
            if ((application.Status ?? "Pending") != "Accepted")
            {
                TempData["JobOfferError"] = "L'entretien ne peut être proposé que pour une candidature acceptée.";
                return RedirectToAction(nameof(Applications), new { id = application.JobId });
            }

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(timeSlot) || string.IsNullOrWhiteSpace(message))
            {
                TempData["JobOfferError"] = "Tous les champs sont requis pour proposer un entretien.";
                return RedirectToAction(nameof(ProposeInterview), new { applicationId });
            }

            var payload = new InterviewProposalPayload
            {
                ApplicationId = application.ApplicationId,
                JobId = application.JobId,
                JobTitle = application.Job.Title,
                AlumniId = application.Job.AlumniId,
                AlumniName = application.Job.Alumni.FullName,
                StudentId = application.StudentId,
                StudentName = application.Student.FullName,
                Subject = subject.Trim(),
                TimeSlot = timeSlot.Trim(),
                Message = message.Trim()
            };

            await _notificationService.CreateAsync(
                application.StudentId,
                "InterviewProposal",
                JsonSerializer.Serialize(payload),
                false,
                $"Proposition d'entretien - {application.Job.Title}");

            TempData["JobOfferSuccess"] = "Proposition d'entretien envoyée à l'étudiant.";
            return RedirectToAction(nameof(Applications), new { id = application.JobId });
        }

        private void PopulateOfferLookups(JobOffer? current = null)
        {
            ViewBag.Degrees = _context.Degrees.OrderBy(d => d.Name).Select(d => d.Name).ToList();
            ViewBag.Levels = _context.Students
                .Where(s => s.Level != null)
                .Select(s => s.Level!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            ViewBag.Skills = _context.Skills.OrderBy(s => s.Name).ToList();
            ViewBag.Interests = _context.Interests.OrderBy(i => i.Name).ToList();
            ViewBag.SelectedSkillIds = ParseCsvIds(current?.RequiredSkillsCsv);
            ViewBag.SelectedInterestIds = ParseCsvIds(current?.RequiredInterestsCsv);
        }

        private async Task<StudentProfileSnapshot?> BuildStudentProfileSnapshotAsync(int userId)
        {
            var studentUser = await _context.Users
                .Include(u => u.Student)
                    .ThenInclude(s => s.Degree)
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.UserInterests)
                    .ThenInclude(ui => ui.Interest)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (studentUser == null)
            {
                return null;
            }

            return new StudentProfileSnapshot
            {
                DegreeName = studentUser.Student?.Degree?.Name,
                Level = studentUser.Student?.Level,
                Field = studentUser.Student?.Field,
                SkillIds = studentUser.UserSkills.Select(us => us.SkillId).ToList(),
                InterestIds = studentUser.UserInterests.Select(ui => ui.InterestId).ToList(),
                SkillNames = studentUser.UserSkills
                    .Where(us => us.Skill != null)
                    .Select(us => us.Skill.Name)
                    .ToList(),
                InterestNames = studentUser.UserInterests
                    .Where(ui => ui.Interest != null)
                    .Select(ui => ui.Interest.Name)
                    .ToList()
            };
        }

        private JobOfferFeedItemViewModel BuildFeedItem(JobOffer offer, StudentProfileSnapshot? profile)
        {
            var score = 0;
            var reasons = new List<string>();

            static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

            if (profile != null)
            {
                var requiredDegree = Normalize(offer.RequiredDegree);
                var requiredLevel = Normalize(offer.RequiredLevel);
                var requiredField = Normalize(offer.RequiredField);
                var profileDegree = Normalize(profile.DegreeName);
                var profileLevel = Normalize(profile.Level);
                var profileField = Normalize(profile.Field);

                if (!string.IsNullOrWhiteSpace(requiredDegree) && requiredDegree == profileDegree)
                {
                    score += 3;
                    reasons.Add($"Diplôme: {offer.RequiredDegree}");
                }

                if (!string.IsNullOrWhiteSpace(requiredLevel) && requiredLevel == profileLevel)
                {
                    score += 2;
                    reasons.Add($"Niveau: {offer.RequiredLevel}");
                }

                if (!string.IsNullOrWhiteSpace(requiredField) && profileField.Contains(requiredField))
                {
                    score += 2;
                    reasons.Add($"Filière: {offer.RequiredField}");
                }

                var requiredSkillIds = ParseCsvIds(offer.RequiredSkillsCsv);
                var requiredInterestIds = ParseCsvIds(offer.RequiredInterestsCsv);

                var matchedSkills = requiredSkillIds.Intersect(profile.SkillIds).Count();
                var matchedInterests = requiredInterestIds.Intersect(profile.InterestIds).Count();

                if (matchedSkills == 0 && profile.SkillNames.Count > 0 && !string.IsNullOrWhiteSpace(offer.RequiredSkillsCsv))
                {
                    var requiredSkillNames = offer.RequiredSkillsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    matchedSkills = requiredSkillNames.Count(required =>
                        profile.SkillNames.Any(name =>
                            Normalize(name) == Normalize(required) ||
                            Normalize(name).Contains(Normalize(required)) ||
                            Normalize(required).Contains(Normalize(name))));
                }

                if (matchedInterests == 0 && profile.InterestNames.Count > 0 && !string.IsNullOrWhiteSpace(offer.RequiredInterestsCsv))
                {
                    var requiredInterestNames = offer.RequiredInterestsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    matchedInterests = requiredInterestNames.Count(required =>
                        profile.InterestNames.Any(name =>
                            Normalize(name) == Normalize(required) ||
                            Normalize(name).Contains(Normalize(required)) ||
                            Normalize(required).Contains(Normalize(name))));
                }

                if (matchedSkills > 0)
                {
                    score += matchedSkills * 2;
                    reasons.Add($"{matchedSkills} compétence(s) correspondante(s)");
                }

                if (matchedInterests > 0)
                {
                    score += matchedInterests;
                    reasons.Add($"{matchedInterests} centre(s) d'intérêt correspondant(s)");
                }
            }

            return new JobOfferFeedItemViewModel
            {
                JobId = offer.JobId,
                OfferTitle = offer.Title,
                Description = offer.Description,
                Type = offer.Type,
                Location = offer.Location,
                CreatedAt = offer.CreatedAt,
                AlumniId = offer.AlumniId,
                PublisherName = offer.Alumni.FullName,
                PublisherEmail = offer.Alumni.Email,
                ApplicationsCount = offer.Applications.Count,
                MatchScore = score,
                MatchSummary = reasons.Count > 0 ? string.Join(" · ", reasons.Distinct()) : string.Empty
            };
        }

        private static List<int> ParseCsvIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return new List<int>();
            }

            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        private static string? JoinSelectedIds(IEnumerable<string> values)
        {
            var ids = values
                .Where(x => int.TryParse(x, out _))
                .Select(int.Parse)
                .Distinct()
                .ToList();

            return ids.Count == 0 ? null : string.Join(',', ids);
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

        private sealed class StudentProfileSnapshot
        {
            public string? DegreeName { get; set; }
            public string? Level { get; set; }
            public string? Field { get; set; }
            public List<int> SkillIds { get; set; } = new();
            public List<int> InterestIds { get; set; } = new();
            public List<string> SkillNames { get; set; } = new();
            public List<string> InterestNames { get; set; } = new();
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
