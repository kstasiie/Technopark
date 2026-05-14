namespace Technopark.Models;
public class ProjectRole
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<TeamMember> TeamMembers { get; set; } = [];
}