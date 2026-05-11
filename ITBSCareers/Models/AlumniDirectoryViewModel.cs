namespace IBSTCareers.Models;

public class AlumniDirectoryViewModel
{
    public string? Query { get; set; }
    public string? Visibility { get; set; }
    public List<AlumniDirectoryItemViewModel> Alumni { get; set; } = new();

    public int TotalCount { get; set; }
    public int PublicCount { get; set; }
    public int PrivateCount { get; set; }
    public int PendingContactRequestsCount { get; set; }
}

public class AlumniDirectoryItemViewModel
{
    public int AlumniId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DegreeName { get; set; }
    public string? Field { get; set; }
    public string? CompanyName { get; set; }
    public string? Position { get; set; }
    public int? ExperienceYears { get; set; }
    public bool IsContactPublic { get; set; }
    public bool CanMessage { get; set; }
    public bool HasPendingContactRequest { get; set; }
    public int? ConversationId { get; set; } = null;
    public string ActionLabel { get; set; } = "Contacter";
    public string StatusLabel { get; set; } = "Privé";
    public string SpecialityLabel { get; set; } = string.Empty;
    public string Initials { get; set; } = "A";
}
