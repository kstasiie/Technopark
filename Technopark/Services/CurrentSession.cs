namespace Technopark.Services;

public static class CurrentSession
{
    public static int UserId { get; set; }
    public static string Login { get; set; } = "";
    public static string Role { get; set; } = "";
    public static string FullName { get; set; } = ""; // заполняется из профиля

    public static bool IsAdmin => Role == "Admin";
    public static bool IsMentor => Role == "Mentor";
    public static bool IsStudent => Role == "Student";

    public static void Clear()
    {
        UserId = 0; Login = ""; Role = ""; FullName = "";
    }
}