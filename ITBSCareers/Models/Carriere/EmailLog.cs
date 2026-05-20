using System;

namespace ITBSCareers.Models.Carriere;

public partial class EmailLog
{
    public int EmailLogId { get; set; }

    public int? UserId { get; set; }

    public string? ToEmail { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
