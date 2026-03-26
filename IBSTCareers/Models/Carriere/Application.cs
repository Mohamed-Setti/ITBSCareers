using System;
using System.Collections.Generic;

namespace IBSTCareers.Models.Carriere;

public partial class Application
{
    public int ApplicationId { get; set; }

    public int JobId { get; set; }

    public int StudentId { get; set; }

    public int? Cvid { get; set; }

    public string? Status { get; set; }

    public DateTime? AppliedAt { get; set; }

    public virtual Cv? Cv { get; set; }

    public virtual JobOffer Job { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
