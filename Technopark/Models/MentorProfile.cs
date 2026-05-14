namespace Technopark.Models;
public class MentorProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string Position { get; set; } = "";

    public int DirectionId { get; set; }
    public Direction? Direction { get; set; }

    // Конфиденциальные (только Admin)
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    public ICollection<Project> Projects { get; set; } = [];
}