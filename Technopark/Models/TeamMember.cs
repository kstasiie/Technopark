namespace Technopark.Models;
public class TeamMember
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public int StudentId { get; set; }
    public StudentProfile? Student { get; set; }

    public int RoleId { get; set; }
    public ProjectRole? Role { get; set; }

    public DateTime InclusionDate { get; set; } = DateTime.Today;
}