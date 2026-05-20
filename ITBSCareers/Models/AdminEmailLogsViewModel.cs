namespace IBSTCareers.Models;

public class AdminEmailLogsViewModel
{
    public string? Query { get; set; }
    public int TotalCount { get; set; }
    public int Last24HoursCount { get; set; }
    public int Last14DaysCount { get; set; }
    public List<AdminEmailLogItemViewModel> Logs { get; set; } = new();
}

public class AdminEmailLogItemViewModel
{
    public int EmailLogId { get; set; }
    public int? UserId { get; set; }
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
    public string? ToEmail { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? BodyPreview { get; set; }
    public DateTime CreatedAt { get; set; }
}
