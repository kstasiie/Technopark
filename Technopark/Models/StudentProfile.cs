namespace Technopark.Models;
public class StudentProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }

    // Конфиденциальные (только Admin)
    public DateTime? BirthDate { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
}