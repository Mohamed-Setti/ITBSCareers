namespace IBSTCareers.Models;

public class AlumniCvThequeViewModel
{
    public string? Query { get; set; }
    public string? Degree { get; set; }
    public string? Level { get; set; }
    public string? Skill { get; set; }
    public string? Interest { get; set; }

    public List<string> Degrees { get; set; } = new();
    public List<string> Levels { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public List<string> Interests { get; set; } = new();

    public List<AlumniCvItemViewModel> Results { get; set; } = new();
}

public class AlumniCvItemViewModel
{
    public int Cvid { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? Field { get; set; }
    public string? Level { get; set; }
    public string? CvFilePath { get; set; }
    public DateTime? CvUploadedAt { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Interests { get; set; } = new();
}
