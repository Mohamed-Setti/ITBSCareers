using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class UserSkill
{
    public int UserSkillId { get; set; }

    public int UserId { get; set; }

    public int SkillId { get; set; }

    public virtual Skill Skill { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
