using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;

namespace Technopark.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    private static string GenerateSalt() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    private static string HashPassword(string password, string salt) =>
        Convert.ToBase64String(SHA256.HashData(
            Encoding.UTF8.GetBytes(password + salt)));

    public async Task<User?> LoginAsync(string login, string password)
    {
        var user = await _db.Users
            .Include(u => u.MentorProfile)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.Login == login && u.IsActive);

        if (user == null) return null;
        return HashPassword(password, user.Salt) == user.PasswordHash ? user : null;
    }

    public async Task EnsureAdminExistsAsync()
    {
        if (await _db.Users.AnyAsync()) return;

        var salt = GenerateSalt();
        var admin = new User
        {
            Login = "admin",
            Salt = salt,
            PasswordHash = HashPassword("admin123", salt),
            Role = "Admin",
            IsActive = true
        };
        _db.Users.Add(admin);
        await _db.SaveChangesAsync();
    }

    public static string GenerateSaltPublic() => GenerateSalt();
    public static string HashPasswordPublic(string password, string salt) =>
        HashPassword(password, salt);
}