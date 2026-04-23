namespace IBSTCareers.Models;

public class JobOfferFeedViewModel
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Location { get; set; }
    public string? Publisher { get; set; }

    public List<string> Types { get; set; } = new() { "Stage", "Emploi" };
    public List<JobOfferFeedItemViewModel> Suggestions { get; set; } = new();
    public List<JobOfferFeedItemViewModel> Results { get; set; } = new();
}

public class JobOfferFeedItemViewModel
{
    public int JobId { get; set; }
    public string OfferTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int AlumniId { get; set; }
    public string PublisherName { get; set; } = string.Empty;
    public string PublisherEmail { get; set; } = string.Empty;
    public int ApplicationsCount { get; set; }
    public int MatchScore { get; set; }
    public string MatchSummary { get; set; } = string.Empty;
}
