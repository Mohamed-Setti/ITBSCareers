using System.ComponentModel.DataAnnotations;

namespace ITBSCareers.Models.Carriere;

public partial class MentorshipRequest
{
    public int MentorshipRequestId { get; set; }
    public int StudentId { get; set; }
    public int AlumniId { get; set; }
    [Required, StringLength(20)]
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ReviewedAt { get; set; }

    public virtual User Student { get; set; } = null!;
    public virtual User Alumni { get; set; } = null!;
}

public partial class ContactRequest : MentorshipRequest
{
}
