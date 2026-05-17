using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class ProjectDetailsPage : Page
    {
        private readonly int _projectId;
        private Project? _project;

        public ProjectDetailsPage(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            Loaded += async (s, e) => await LoadAsync();
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (_project == null || !CurrentSession.IsAdmin) return;

            var result = MessageBox.Show(
                $"Удалить проект «{_project.Name}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            db.Projects.Remove(_project);
            await db.SaveChangesAsync();

            // Возврат назад
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private async Task LoadAsync()
        {
            using var db = new AppDbContext();
            _project = await db.Projects
                .Include(p => p.Direction)
                .Include(p => p.Status)
                .Include(p => p.Mentor)
                .Include(p => p.Team).ThenInclude(t => t!.Members)
                    .ThenInclude(m => m.Student)
                .Include(p => p.Team).ThenInclude(t => t!.Members)
                    .ThenInclude(m => m.Role)
                .Include(p => p.ContestParticipations).ThenInclude(cp => cp.Contest)
                    .ThenInclude(c => c!.Level)
                .Include(p => p.ContestParticipations).ThenInclude(cp => cp.Result)
                .FirstOrDefaultAsync(p => p.Id == _projectId);

            if (_project == null) return;

            ProjectNameText.Text = _project.Name;
            DescriptionText.Text = string.IsNullOrWhiteSpace(_project.Description)
                ? "Описание отсутствует" : _project.Description;
            StatusText.Text = _project.Status?.Name ?? "";
            DirectionText.Text = $"📁 {_project.Direction?.Name}";

            MentorLink.Text = _project.Mentor?.FullName ?? "Не назначен";
            MentorLink.Tag = _project.MentorId;

            TeamLink.Text = _project.Team?.Name ?? "Не назначена";
            TeamLink.Tag = _project.TeamId;

            StartDateText.Text = _project.StartDate.ToString("dd.MM.yyyy");
            EndDateText.Text = _project.PlannedEndDate?.ToString("dd.MM.yyyy") ?? "Не указано";

            // Состав команды
            var members = _project.Team?.Members.Select(m => new
            {
                StudentId = m.StudentId,
                Name = m.Student?.FullName ?? "",
                Role = m.Role?.Name ?? ""
            }).ToList();
            MembersList.ItemsSource = members;

            // Конкурсы
            var contests = _project.ContestParticipations.Select(cp => new
            {
                ContestId = cp.ContestId,
                ContestName = cp.Contest?.Name ?? "",
                Level = cp.Contest?.Level?.Name ?? "",
                Result = cp.Result?.Name ?? "—",
                Place = cp.Place?.ToString() ?? "—"
            }).ToList();
            ContestsGrid.ItemsSource = contests;

            // Право редактирования
            bool canEdit = CurrentSession.IsAdmin;
            if (CurrentSession.IsMentor)
            {
                var mentor = await db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == CurrentSession.UserId);
                canEdit = mentor != null && _project.MentorId == mentor.Id;
            }
            EditBtn.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            DeleteBtn.Visibility = CurrentSession.IsAdmin
                ? Visibility.Visible : Visibility.Collapsed;
            AddToContestBtn.Visibility = canEdit
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void AddToContestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_project == null) return;
            var dialog = new Dialogs.AddToContestDialog(_project.Id);
            if (dialog.ShowDialog() == true)
                await LoadAsync();
        }
        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_project == null) return;
            var dialog = new Dialogs.ProjectDialog(_project, true);
            if (dialog.ShowDialog() == true) await LoadAsync();
        }
        private void ContestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContestsGrid.SelectedItem is { } item)
            {
                var contestId = (int)item.GetType().GetProperty("ContestId")!.GetValue(item)!;
                NavigationService?.Navigate(new ContestDetailsPage(contestId));
            }
        }
        private void MentorLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (_project?.MentorId > 0)
                NavigationService?.Navigate(new MentorDetailsPage(_project.MentorId));
        }

        private void TeamLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (_project?.TeamId > 0)
                NavigationService?.Navigate(new TeamDetailsPage(_project.TeamId));
        }

        private void MemberLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.Tag is int studentId)
                NavigationService?.Navigate(new StudentDetailsPage(studentId));
        }
    }
}