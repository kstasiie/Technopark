namespace Technopark.Models;
public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Salt { get; set; } = "";
    public string Role { get; set; } = "Student"; // "Admin", "Mentor", "Student"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MentorProfile? MentorProfile { get; set; }
    public StudentProfile? StudentProfile { get; set; }
}