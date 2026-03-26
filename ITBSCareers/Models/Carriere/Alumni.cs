using System;
using System.Collections.Generic;

namespace IBSTCareers.Models.Carriere;

public partial class Alumni
{
    public int AlumniId { get; set; }

    public string? CompanyName { get; set; }

    public string? Position { get; set; }

    public int? ExperienceYears { get; set; }

    public virtual User AlumniNavigation { get; set; } = null!;
}
