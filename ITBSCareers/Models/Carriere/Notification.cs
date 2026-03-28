using System;
using System.Collections.Generic;

namespace ITBSCareers.Models.Carriere;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string? Content { get; set; }

    public string? Type { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
