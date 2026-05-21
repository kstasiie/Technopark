using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;

namespace Technopark.Views
{
    public partial class AnalyticsPage : Page
    {

        public AnalyticsPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            await LoadDirectionsAsync();
            await LoadMentorsAsync();
            await LoadContestsAsync();
            await LoadDynamicsAsync();
        }

        // 1. Активность по направлениям
        private async Task LoadDirectionsAsync()
        {
            using var db = new AppDbContext();
            var data = await db.Directions
                .Select(d => new
                {
                    Label = d.Name,
                    Value = d.Projects.Count
                })
                .OrderByDescending(d => d.Value)
                .ToListAsync();

            int max = data.Count == 0 ? 1 : Math.Max(data.Max(d => d.Value), 1);
            const double maxBarWidth = 400;

            DirectionsBars.ItemsSource = data.Select(d => new BarItem
            {
                Label = d.Label,
                Value = d.Value,
                BarWidth = d.Value * maxBarWidth / max
            }).ToList();
        }

        // 2. Эффективность наставников
        private async Task LoadMentorsAsync()
        {
            using var db = new AppDbContext();
            var mentors = await db.MentorProfiles
                .Include(m => m.Direction)
                .Include(m => m.Projects).ThenInclude(p => p.Status)
                .Include(m => m.Projects).ThenInclude(p => p.ContestParticipations)
                    .ThenInclude(cp => cp.Result)
                .ToListAsync();

            var rows = mentors.Select(m => new
            {
                Name = m.FullName,
                Direction = m.Direction?.Name ?? "",
                TotalProjects = m.Projects.Count,
                CompletedProjects = m.Projects.Count(p => p.Status?.Name == "Завершён"),
                PrizeCount = m.Projects.SelectMany(p => p.ContestParticipations)
                    .Count(cp => cp.Result != null && cp.Result.Name != "Участник")
            })
            .OrderByDescending(r => r.TotalProjects)
            .ToList();

            MentorsGrid.ItemsSource = rows;
        }

        // 3. Конкурсная активность по годам
        private async Task LoadContestsAsync()
        {
            using var db = new AppDbContext();
            var participations = await db.ContestParticipations
                .Include(cp => cp.Contest)
                .Include(cp => cp.Result)
                .ToListAsync();

            var byYear = participations
                .GroupBy(cp => cp.Contest?.Date.Year ?? 0)
                .Where(g => g.Key > 0)
                .Select(g => new
                {
                    Year = g.Key,
                    Total = g.Count(),
                    Prizes = g.Count(cp =>
                        cp.Result != null && cp.Result.Name != "Участник")
                })
                .OrderBy(x => x.Year)
                .ToList();

            int max = byYear.Count == 0 ? 1 : Math.Max(byYear.Max(x => x.Total), 1);
            const double maxBarWidth = 400;

            ContestsBars.ItemsSource = byYear.Select(x => new BarItem
            {
                Label = x.Year.ToString(),
                Value = x.Total,
                BarWidth = x.Total * maxBarWidth / max,
                PrizeWidth = x.Prizes * maxBarWidth / max,
                TotalText = $"{x.Total} участий",
                PrizesText = $"{x.Prizes} призовых"
            }).ToList();
        }

        // 4. Динамика по годам
        private async Task LoadDynamicsAsync()
        {
            using var db = new AppDbContext();

            var projects = await db.Projects
                .Include(p => p.Status)
                .ToListAsync();
            var teams = await db.Teams.ToListAsync();

            var allYears = projects.Select(p => p.StartDate.Year)
                .Concat(teams.Select(t => t.FormationYear))
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            var rows = allYears.Select(y => new
            {
                Year = y,
                NewProjects = projects.Count(p => p.StartDate.Year == y),
                NewTeams = teams.Count(t => t.FormationYear == y),
                ActiveProjects = projects.Count(p =>
                    p.StartDate.Year == y && p.Status?.Name == "В разработке")
            }).ToList();

            DynamicsGrid.ItemsSource = rows;
        }
    }

    public class BarItem
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
        public double BarWidth { get; set; }
        public double PrizeWidth { get; set; }
        public string TotalText { get; set; } = "";
        public string PrizesText { get; set; } = "";
    }
}