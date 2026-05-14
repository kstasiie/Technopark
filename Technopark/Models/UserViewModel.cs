namespace Technopark.Models;

public class UserViewModel
{
    public int UserId { get; set; }
    public string Login { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }

    public string DisplayName { get; set; } = "";
    public string PositionOrClass { get; set; } = "";
    public string DirectionName { get; set; } = "";

    public string RoleDisplay => Role switch
    {
        "Admin" => "Администратор",
        "Mentor" => "Наставник",
        "Student" => "Участник",
        _ => Role
    };
}