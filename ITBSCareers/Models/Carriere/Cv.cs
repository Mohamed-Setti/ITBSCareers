using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class Cv
{
    public int Cvid { get; set; }

    public int UserId { get; set; }

    public string FilePath { get; set; } = null!;

    public DateTime? UploadedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual User User { get; set; } = null!;
}
