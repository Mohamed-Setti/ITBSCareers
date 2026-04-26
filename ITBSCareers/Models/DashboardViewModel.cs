namespace IBSTCareers.Models;

public class DashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public int ExperiencesCount { get; set; }
    public int SkillsCount { get; set; }
    public int InterestsCount { get; set; }

    public int StudentApplicationsCount { get; set; }
    public int NewOffersCount { get; set; }
    public List<JobOfferFeedItemViewModel> HotOpportunities { get; set; } = new();

    public int AlumniPublishedOffersCount { get; set; }
    public int AlumniApplicationsReceivedCount { get; set; }
    public int AlumniActiveMenteesCount { get; set; }

    public bool IsStudent { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsVerifiedAlumni { get; set; }
    public string? AlumniRequestStatus { get; set; }
    public int PendingAlumniRequestsCount { get; set; }
}
