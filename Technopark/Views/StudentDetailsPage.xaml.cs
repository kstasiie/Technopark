using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class StudentDetailsPage : Page
    {
        private readonly int _studentId;
        private StudentProfile? _student;

        public StudentDetailsPage(int studentId)
        {
            InitializeComponent();
            _studentId = studentId;
            Loaded += async (s, e) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            using var db = new AppDbContext();
            _student = await db.StudentProfiles
                .Include(s => s.User)
                .Include(s => s.TeamMemberships).ThenInclude(tm => tm.Team)
                    .ThenInclude(t => t!.Projects)
                .Include(s => s.TeamMemberships).ThenInclude(tm => tm.Role)
                .FirstOrDefaultAsync(s => s.Id == _studentId);

            if (_student == null) return;

            FullNameText.Text = _student.FullName;
            AvatarText.Text = _student.LastName.Length > 0 && _student.FirstName.Length > 0
                ? $"{_student.LastName[0]}{_student.FirstName[0]}" : "?";

            // Команды
            TeamsList.ItemsSource = _student.TeamMemberships.Select(tm => new
            {
                Id = tm.TeamId,
                Name = tm.Team?.Name ?? "",
                Role = tm.Role?.Name ?? ""
            }).ToList();

            // Конкурсы через команды
            var projectIds = _student.TeamMemberships
                .SelectMany(tm => tm.Team!.Projects.Select(p => p.Id))
                .Distinct().ToList();

            var participations = await db.ContestParticipations
                .Include(cp => cp.Contest)
                .Include(cp => cp.Project)
                .Include(cp => cp.Result)
                .Where(cp => projectIds.Contains(cp.ProjectId))
                .ToListAsync();

            ContestsGrid.ItemsSource = participations.Select(cp => new
            {
                ContestName = cp.Contest?.Name ?? "",
                ProjectName = cp.Project?.Name ?? "",
                Result = cp.Result?.Name ?? "—",
                Place = cp.Place?.ToString() ?? "—"
            }).ToList();

            // Личные данные — только Admin и наставник
            if (!CurrentSession.IsStudent)
            {
                BirthDateText.Text = $"Дата рождения: {_student.BirthDate?.ToString("dd.MM.yyyy") ?? "не указана"}";
                EmailText.Text = $"Email: {_student.Email ?? "не указан"}";
                PhoneText.Text = $"Телефон: {_student.Phone ?? "не указан"}";
            }
            else
            {
                ContactsBlock.Visibility = Visibility.Collapsed;
            }

            bool isOwnProfile = _student.UserId == CurrentSession.UserId;
            EditBtn.Visibility = (CurrentSession.IsAdmin || isOwnProfile)
                ? Visibility.Visible : Visibility.Collapsed;
            DeleteBtn.Visibility = (CurrentSession.IsAdmin && !isOwnProfile)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (_student?.User == null) return;
            var user = await db.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == _student.UserId);
            if (user == null) return;
            var dialog = new Dialogs.UserDialog(user);
            if (dialog.ShowDialog() == true) await LoadAsync();
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (_student == null) return;
            var result = MessageBox.Show($"Удалить участника «{_student.FullName}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            var user = await db.Users.FindAsync(_student.UserId);
            if (user != null) db.Users.Remove(user);
            await db.SaveChangesAsync();
            if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
        }

        private void TeamLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.Tag is int teamId)
                NavigationService?.Navigate(new TeamDetailsPage(teamId));
        }
    }
}