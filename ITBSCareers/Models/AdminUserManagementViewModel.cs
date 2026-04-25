using ITBSCareers.Models.Carriere;

namespace IBSTCareers.Models;

public class AdminUserManagementViewModel
{
    public string? Query { get; set; }
    public string? RoleFilter { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<AdminUserItemViewModel> Users { get; set; } = new();
}

public class AdminUserItemViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsAlumni { get; set; }
    public bool IsStudent { get; set; }
    public bool IsAdmin { get; set; }
}
