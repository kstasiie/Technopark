namespace Technopark.Models;
public class Direction
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<MentorProfile> Mentors { get; set; } = [];
}