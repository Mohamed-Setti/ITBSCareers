using System;
using System.Collections.Generic;

namespace IBSTCareers.Models.Carriere;

public partial class Student
{
    public int StudentId { get; set; }

    public int DegreeId { get; set; }

    public string? Field { get; set; }

    public string? Level { get; set; }

    public virtual Degree Degree { get; set; } = null!;

    public virtual User StudentNavigation { get; set; } = null!;
}
