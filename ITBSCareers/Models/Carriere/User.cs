using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Alumni? Alumni { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<Cv> Cvs { get; set; } = new List<Cv>();

    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();

    public virtual ICollection<JobOffer> JobOffers { get; set; } = new List<JobOffer>();

    public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Student? Student { get; set; }

    public virtual ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
}
