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
                .Include(u => u.Student)
                    .ThenInclude(s => s.Degree)
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.UserInterests)
                    .ThenInclude(ui => ui.Interest)
                .Include(u => u.Applications)
                .Include(u => u.JobOffers)
                    .ThenInclude(j => j.Applications)
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

            if (roleNames.Contains("Admin"))
            {
                return RedirectToAction("Profile", "Admin");
            }

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

            var newOffersCount = await _context.JobOffers
                .CountAsync(j => j.CreatedAt != null && j.CreatedAt >= DateTime.Now.AddDays(-14));

            var unreadMessagesCount = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId.Value && !m.IsRead && !m.Conversation.IsDeleted);

            var vm = new DashboardViewModel
            {
                FullName = user.FullName,
                Roles = roleNames,
                ExperiencesCount = user.Experiences.Count,
                SkillsCount = user.UserSkills.Count,
                InterestsCount = user.UserInterests.Count,

                StudentApplicationsCount = user.Applications.Count,
                NewOffersCount = newOffersCount,
                UnreadMessagesCount = unreadMessagesCount,
                HotOpportunities = roleNames.Contains("Student")
                    ? await BuildHotOpportunitiesAsync(user)
                    : new List<JobOfferFeedItemViewModel>(),

                AlumniPublishedOffersCount = user.JobOffers.Count,
                AlumniApplicationsReceivedCount = user.JobOffers.SelectMany(o => o.Applications).Count(),
                AlumniActiveMenteesCount = 0,

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

        private async Task<List<JobOfferFeedItemViewModel>> BuildHotOpportunitiesAsync(User user)
        {
            var studentProfile = new StudentProfileSnapshot
            {
                DegreeName = user.Student?.Degree?.Name,
                Level = user.Student?.Level,
                Field = user.Student?.Field,
                SkillIds = user.UserSkills.Select(us => us.SkillId).ToList(),
                InterestIds = user.UserInterests.Select(ui => ui.InterestId).ToList(),
                SkillNames = user.UserSkills.Where(us => us.Skill != null).Select(us => us.Skill.Name).ToList(),
                InterestNames = user.UserInterests.Where(ui => ui.Interest != null).Select(ui => ui.Interest.Name).ToList()
            };

            var offers = await _context.JobOffers
                .Include(o => o.Alumni)
                .Include(o => o.Applications)
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .ToListAsync();

            return offers
                .Select(o => BuildFeedItem(o, studentProfile))
                .Where(x => x.MatchScore > 0)
                .OrderByDescending(x => x.MatchScore)
                .ThenByDescending(x => x.CreatedAt)
                .Take(3)
                .ToList();
        }

        private JobOfferFeedItemViewModel BuildFeedItem(JobOffer offer, StudentProfileSnapshot? profile)
        {
            var score = 0;
            var reasons = new List<string>();
            var matchedKeywords = new List<string>();

            static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

            if (profile != null)
            {
                var requiredDegree = Normalize(offer.RequiredDegree);
                var requiredLevel = Normalize(offer.RequiredLevel);
                var requiredField = Normalize(offer.RequiredField);
                var profileDegree = Normalize(profile.DegreeName);
                var profileLevel = Normalize(profile.Level);
                var profileField = Normalize(profile.Field);
                var offerText = Normalize($"{offer.Title} {offer.Description} {offer.Type} {offer.Location}");

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
                            Normalize(required).Contains(Normalize(name)) ||
                            offerText.Contains(Normalize(name))));
                }

                if (matchedInterests == 0 && profile.InterestNames.Count > 0 && !string.IsNullOrWhiteSpace(offer.RequiredInterestsCsv))
                {
                    var requiredInterestNames = offer.RequiredInterestsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    matchedInterests = requiredInterestNames.Count(required =>
                        profile.InterestNames.Any(name =>
                            Normalize(name) == Normalize(required) ||
                            Normalize(name).Contains(Normalize(required)) ||
                            Normalize(required).Contains(Normalize(name)) ||
                            offerText.Contains(Normalize(name))));
                }

                // Fallback for older offers where criteria are written only in text.
                if (matchedSkills == 0 && profile.SkillNames.Count > 0)
                {
                    matchedSkills = profile.SkillNames.Count(name => offerText.Contains(Normalize(name)));
                }

                if (matchedInterests == 0 && profile.InterestNames.Count > 0)
                {
                    matchedInterests = profile.InterestNames.Count(name => offerText.Contains(Normalize(name)));
                }

                if (matchedSkills > 0)
                {
                    score += matchedSkills * 2;
                    reasons.Add($"{matchedSkills} compétence(s) correspondante(s)");
                    matchedKeywords.AddRange(profile.SkillNames.Where(n => offerText.Contains(Normalize(n))));
                }

                if (matchedInterests > 0)
                {
                    score += matchedInterests;
                    reasons.Add($"{matchedInterests} centre(s) d'intérêt correspondant(s)");
                    matchedKeywords.AddRange(profile.InterestNames.Where(n => offerText.Contains(Normalize(n))));
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
