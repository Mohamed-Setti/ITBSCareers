namespace IBSTCareers.Models;

public class SelectSkillsInterestsViewModel
{
    public int UserId { get; set; }
    public List<CheckboxItem> Skills { get; set; } = new();
    public List<CheckboxItem> Interests { get; set; } = new();
}

public class CheckboxItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsSelected { get; set; }
}