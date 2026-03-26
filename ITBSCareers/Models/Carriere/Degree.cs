using System;
using System.Collections.Generic;

namespace IBSTCareers.Models.Carriere;

public partial class Degree
{
    public int DegreeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
