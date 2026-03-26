using System;
using System.Collections.Generic;

namespace IBSTCareers.Models.Carriere;

public partial class Interest
{
    public int InterestId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
