using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class Skill
{
    public int SkillId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
}
