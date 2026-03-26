using System;
using System.Collections.Generic;

namespace IBSTCareers.Models.Carriere;

public partial class Experience
{
    public int ExperienceId { get; set; }

    public int? UserId { get; set; }

    public string? Title { get; set; }

    public string? Company { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Description { get; set; }

    public virtual User? User { get; set; }
}
