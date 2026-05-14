using Microsoft.EntityFrameworkCore;
using Technopark.Models;

namespace Technopark.Data;

public class AppDbContext : DbContext
{
    // Справочники
    public DbSet<ProjectStatus> ProjectStatuses { get; set; }
    public DbSet<ContestLevel> ContestLevels { get; set; }
    public DbSet<ParticipationResult> ParticipationResults { get; set; }
    public DbSet<ProjectRole> ProjectRoles { get; set; }
    public DbSet<MaterialType> MaterialTypes { get; set; }
    public DbSet<Direction> Directions { get; set; }

    // Основные сущности
    public DbSet<User> Users { get; set; }
    public DbSet<MentorProfile> MentorProfiles { get; set; }
    public DbSet<StudentProfile> StudentProfiles { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Contest> Contests { get; set; }
    public DbSet<ContestParticipation> ContestParticipations { get; set; }
    public DbSet<ProjectMaterial> ProjectMaterials { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(
            "Host=localhost;Database=technopark;Username=postgres;Password=techno"
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Справочники — начальные данные
        modelBuilder.Entity<Direction>().HasData(
            new Direction { Id = 1, Name = "Аэронет" },
            new Direction { Id = 2, Name = "Автонет" },
            new Direction { Id = 3, Name = "Нейронет" },
            new Direction { Id = 4, Name = "Маринет" },
            new Direction { Id = 5, Name = "Энерджинет" },
            new Direction { Id = 6, Name = "Хелснет" },
            new Direction { Id = 7, Name = "Технет" },
            new Direction { Id = 8, Name = "Кружковое движение" }
        );

        modelBuilder.Entity<ProjectStatus>().HasData(
            new ProjectStatus { Id = 1, Name = "В разработке" },
            new ProjectStatus { Id = 2, Name = "Завершён" },
            new ProjectStatus { Id = 3, Name = "Приостановлен" }
        );

        modelBuilder.Entity<ContestLevel>().HasData(
            new ContestLevel { Id = 1, Name = "Муниципальный" },
            new ContestLevel { Id = 2, Name = "Региональный" },
            new ContestLevel { Id = 3, Name = "Всероссийский" },
            new ContestLevel { Id = 4, Name = "Международный" }
        );

        modelBuilder.Entity<ParticipationResult>().HasData(
            new ParticipationResult { Id = 1, Name = "Участник" },
            new ParticipationResult { Id = 2, Name = "Призёр" },
            new ParticipationResult { Id = 3, Name = "Победитель" }
        );

        modelBuilder.Entity<ProjectRole>().HasData(
            new ProjectRole { Id = 1, Name = "Руководитель проекта" },
            new ProjectRole { Id = 2, Name = "Разработчик" },
            new ProjectRole { Id = 3, Name = "Исследователь" },
            new ProjectRole { Id = 4, Name = "Дизайнер" }
        );

        modelBuilder.Entity<MaterialType>().HasData(
            new MaterialType { Id = 1, Name = "Техническое описание" },
            new MaterialType { Id = 2, Name = "Презентация" },
            new MaterialType { Id = 3, Name = "Фото" },
            new MaterialType { Id = 4, Name = "Видео" }
        );
    }
}