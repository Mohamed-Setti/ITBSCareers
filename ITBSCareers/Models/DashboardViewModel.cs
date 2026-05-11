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
    public int UnreadMessagesCount { get; set; }
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

public class AlumniRequestListItemViewModel
{
    public int AlumniRequestId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Position { get; set; }
    public string? ProofFilePath { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
