using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class PortfolioPage : Page
    {

        public PortfolioPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            using var db = new AppDbContext();
            var student = await db.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == CurrentSession.UserId);

            if (student == null) return;

            // Профиль
            FullNameText.Text = student.FullName;
            AvatarText.Text = student.FirstName.Length > 0 && student.LastName.Length > 0
                ? $"{student.LastName[0]}{student.FirstName[0]}" : "?";

            // Проекты участника
            var memberships = await db.TeamMembers
                .Include(tm => tm.Team).ThenInclude(t => t.Projects)
                    .ThenInclude(p => p.Direction)
                .Include(tm => tm.Team).ThenInclude(t => t.Projects)
                    .ThenInclude(p => p.Status)
                .Include(tm => tm.Role)
                .Where(tm => tm.StudentId == student.Id)
                .ToListAsync();

            var projectRows = memberships
                .SelectMany(tm => tm.Team.Projects.Select(p => new
                {
                    ProjectName = p.Name,
                    Direction = p.Direction?.Name ?? "",
                    TeamRole = tm.Role?.Name ?? "",
                    Status = p.Status?.Name ?? ""
                }))
                .Distinct()
                .ToList();

            ProjectsGrid.ItemsSource = projectRows;

            // Конкурсы
            var teamIds = memberships.Select(tm => tm.TeamId).Distinct().ToList();
            var projectIds = memberships
                .SelectMany(tm => tm.Team.Projects.Select(p => p.Id))
                .Distinct().ToList();

            var participations = await db.ContestParticipations
                .Include(cp => cp.Contest).ThenInclude(c => c.Level)
                .Include(cp => cp.Project)
                .Include(cp => cp.Result)
                .Where(cp => projectIds.Contains(cp.ProjectId))
                .ToListAsync();

            var contestRows = participations.Select(cp => new
            {
                ContestName = cp.Contest?.Name ?? "",
                Level = cp.Contest?.Level?.Name ?? "",
                ProjectName = cp.Project?.Name ?? "",
                Result = cp.Result?.Name ?? "—",
                Place = cp.Place.HasValue ? cp.Place.ToString() : "—"
            }).ToList();

            ContestsGrid.ItemsSource = contestRows;

            // Статистика
            var converter = new ColorConverter();
            AddStat("Проектов", projectRows.Count.ToString(), "#534AB7", converter);
            AddStat("Конкурсов", contestRows.Count.ToString(), "#0F6E56", converter);
            var prizes = contestRows.Count(c => c.Result != "Участник" && c.Result != "—");
            AddStat("Призовых мест", prizes.ToString(), "#993C1D", converter);
        }

        private void AddStat(string label, string value, string colorHex,
            ColorConverter converter)
        {
            var color = (Color)converter.ConvertFrom(colorHex)!;
            var border = (Color)converter.ConvertFrom("#E5E7EB")!;

            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(border),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 12, 0),
                Padding = new Thickness(18, 14, 18, 14)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(0, 0, 0, 4)
            });
            stack.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 28,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(color)
            });
            card.Child = stack;
            StatsGrid.Children.Add(card);
        }
    }
}