namespace Technopark.Models;
public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int FormationYear { get; set; } = DateTime.Now.Year;

    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<TeamMember> Members { get; set; } = [];
}