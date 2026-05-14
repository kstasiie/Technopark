namespace Technopark.Models;
public class ProjectStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<Project> Projects { get; set; } = [];
}