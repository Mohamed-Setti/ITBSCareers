namespace IBSTCareers.Models;

public class AdminProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();

    public int TotalUsers { get; set; }
    public int StudentsCount { get; set; }
    public int AlumniCount { get; set; }
    public int AdminsCount { get; set; }
    public int TotalOffers { get; set; }
    public int TotalApplications { get; set; }
    public int PendingAlumniRequests { get; set; }
    public int AcceptedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public int InterviewProposals { get; set; }
    public int NotificationsLast14Days { get; set; }
    public int NewUsersLast14Days { get; set; }
    public int NewOffersLast14Days { get; set; }
}
