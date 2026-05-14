namespace Technopark.Models;
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public int DirectionId { get; set; }
    public Direction? Direction { get; set; }

    public int StatusId { get; set; }
    public ProjectStatus? Status { get; set; }

    public int MentorId { get; set; }
    public MentorProfile? Mentor { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? PlannedEndDate { get; set; }

    public ICollection<ContestParticipation> ContestParticipations { get; set; } = [];
    public ICollection<ProjectMaterial> Materials { get; set; } = [];

    public string StatusDisplay => Status?.Name ?? "";
}