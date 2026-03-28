using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class JobOffer
{
    public int JobId { get; set; }

    public int AlumniId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Type { get; set; }

    public string? Location { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Alumni { get; set; } = null!;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
