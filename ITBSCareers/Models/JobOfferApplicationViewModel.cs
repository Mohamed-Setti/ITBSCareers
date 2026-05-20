namespace IBSTCareers.Models;

public class JobOfferApplicationViewModel
{
    public int ApplicationId { get; set; }
    public int JobId { get; set; }
    public string OfferTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }

    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? Field { get; set; }
    public string? Level { get; set; }
    public string? CvFilePath { get; set; }
    public DateTime? CvUploadedAt { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Interests { get; set; } = new();
}

public class AlumniApplicationsBoardViewModel
{
    public string? Query { get; set; }
    public string Filter { get; set; } = "All";
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int InterviewReadyCount { get; set; }
    public List<AlumniApplicationItemViewModel> Applications { get; set; } = new();
}

public class AlumniApplicationItemViewModel
{
    public int ApplicationId { get; set; }
    public int JobId { get; set; }
    public string OfferTitle { get; set; } = string.Empty;
    public string? OfferType { get; set; }
    public string? OfferLocation { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? Field { get; set; }
    public string? Level { get; set; }
    public string? CvFilePath { get; set; }
    public DateTime? CvUploadedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }
    public int SkillsCount { get; set; }
    public int InterestsCount { get; set; }
    public int ExperiencesCount { get; set; }
    public bool CanProposeInterview { get; set; }
}
