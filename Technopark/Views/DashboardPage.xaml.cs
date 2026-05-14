using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class DashboardPage : Page
    {
        private readonly AppDbContext _db = new();

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            WelcomeText.Text = $"Добрый день, {CurrentSession.FullName}!";
            await LoadStatsAsync();
            await LoadProjectsAsync();
            await LoadContestsAsync();
        }

        private async Task LoadStatsAsync()
        {
            StatsGrid.Children.Clear();

            if (CurrentSession.IsAdmin)
            {
                var projectCount = await _db.Projects.CountAsync();
                var mentorCount = await _db.Users
                    .CountAsync(u => u.Role == "Mentor" && u.IsActive);
                var studentCount = await _db.Users
                    .CountAsync(u => u.Role == "Student" && u.IsActive);
                var contestCount = await _db.Contests.CountAsync();

                AddStatCard("Проектов", projectCount.ToString(), "#534AB7");
                AddStatCard("Наставников", mentorCount.ToString(), "#0F6E56");
                AddStatCard("Участников", studentCount.ToString(), "#993C1D");
                AddStatCard("Конкурсов", contestCount.ToString(), "#185FA5");
            }
            else if (CurrentSession.IsMentor)
            {
                var mentor = await _db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == CurrentSession.UserId);

                if (mentor != null)
                {
                    var myProjects = await _db.Projects
                        .CountAsync(p => p.MentorId == mentor.Id);
                    var myActive = await _db.Projects
                        .CountAsync(p => p.MentorId == mentor.Id
                                      && p.Status!.Name == "В разработке");
                    var myMembers = await _db.TeamMembers
                        .CountAsync(tm => tm.Team.Projects
                            .Any(p => p.MentorId == mentor.Id));

                    AddStatCard("Моих проектов", myProjects.ToString(), "#534AB7");
                    AddStatCard("В разработке", myActive.ToString(), "#0F6E56");
                    AddStatCard("Участников", myMembers.ToString(), "#993C1D");
                }
            }
            else
            {
                var student = await _db.StudentProfiles
                    .FirstOrDefaultAsync(s => s.UserId == CurrentSession.UserId);

                if (student != null)
                {
                    var myProjects = await _db.TeamMembers
                        .CountAsync(tm => tm.StudentId == student.Id);
                    var myContests = await _db.ContestParticipations
                        .CountAsync(cp => cp.Project.Team.Members
                            .Any(m => m.StudentId == student.Id));
                    var myPrizes = await _db.ContestParticipations
                        .CountAsync(cp => cp.Result != null &&
                            cp.Result.Name != "Участник" &&
                            cp.Project.Team.Members
                                .Any(m => m.StudentId == student.Id));

                    AddStatCard("Мои проекты", myProjects.ToString(), "#534AB7");
                    AddStatCard("Конкурсов", myContests.ToString(), "#0F6E56");
                    AddStatCard("Призовых мест", myPrizes.ToString(), "#993C1D");
                }
            }
        }

        private void AddStatCard(string label, string value, string colorHex)
        {
            var converter = new ColorConverter();
            var borderColor = (Color)converter.ConvertFrom("#E5E7EB")!;
            var color = (Color)converter.ConvertFrom(colorHex)!;

            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(borderColor),
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

        private async Task LoadProjectsAsync()
        {
            List<Project> projects;

            if (CurrentSession.IsAdmin)
            {
                projects = await _db.Projects
                    .Include(p => p.Direction)
                    .Include(p => p.Mentor)
                    .Include(p => p.Status)
                    .OrderByDescending(p => p.StartDate)
                    .Take(5)
                    .ToListAsync();
            }
            else if (CurrentSession.IsMentor)
            {
                var mentor = await _db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == CurrentSession.UserId);

                projects = mentor == null ? [] : await _db.Projects
                    .Include(p => p.Direction)
                    .Include(p => p.Mentor)
                    .Include(p => p.Status)
                    .Where(p => p.MentorId == mentor.Id)
                    .OrderByDescending(p => p.StartDate)
                    .Take(5)
                    .ToListAsync();
            }
            else
            {
                var student = await _db.StudentProfiles
                    .FirstOrDefaultAsync(s => s.UserId == CurrentSession.UserId);

                projects = student == null ? [] : await _db.Projects
                    .Include(p => p.Direction)
                    .Include(p => p.Mentor)
                    .Include(p => p.Status)
                    .Where(p => p.Team.Members.Any(m => m.StudentId == student.Id))
                    .OrderByDescending(p => p.StartDate)
                    .Take(5)
                    .ToListAsync();
            }

            ProjectsGrid.ItemsSource = projects;
        }

        private async Task LoadContestsAsync()
        {
            var contests = await _db.Contests
                .Include(c => c.Level)
                .OrderByDescending(c => c.Date)
                .Take(5)
                .ToListAsync();

            ContestsGrid.ItemsSource = contests;
        }
        private void ProjectsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsGrid.SelectedItem is Models.Project project)
                NavigationService?.Navigate(new ProjectDetailsPage(project.Id));
        }

        private void ContestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContestsGrid.SelectedItem is Models.Contest contest)
                NavigationService?.Navigate(new ContestDetailsPage(contest.Id));
        }
    }
}