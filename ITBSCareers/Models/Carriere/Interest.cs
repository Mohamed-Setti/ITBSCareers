using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class Interest
{
    public int InterestId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
}
