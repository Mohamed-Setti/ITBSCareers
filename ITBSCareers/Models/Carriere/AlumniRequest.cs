using System;

namespace ITBSCareers.Models.Carriere;

public partial class AlumniRequest
{
    public int AlumniRequestId { get; set; }

    public int UserId { get; set; }

    public string? CompanyName { get; set; }

    public string? Position { get; set; }

    public string? ProofFilePath { get; set; }

    public string Status { get; set; } = "Pending";

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? CreatedAt { get; set; }
}
