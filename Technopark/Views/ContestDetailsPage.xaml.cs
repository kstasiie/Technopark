using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class ContestDetailsPage : Page
    {
        private readonly int _contestId;
        private Contest? _contest;

        public ContestDetailsPage(int contestId)
        {
            InitializeComponent();
            _contestId = contestId;
            Loaded += async (s, e) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            using var db = new AppDbContext();
            _contest = await db.Contests
                .Include(c => c.Level)
                .Include(c => c.Participations).ThenInclude(p => p.Project).ThenInclude(p => p!.Mentor)
                .Include(c => c.Participations).ThenInclude(p => p.Result)
                .FirstOrDefaultAsync(c => c.Id == _contestId);

            if (_contest == null) return;

            NameText.Text = _contest.Name;
            OrganizerText.Text = $"Организатор: {_contest.Organizer}";
            LevelText.Text = _contest.Level?.Name ?? "";
            DateText.Text = $"📅 {_contest.Date:dd.MM.yyyy}";

            ProjectsGrid.ItemsSource = _contest.Participations.Select(cp => new
            {
                ProjectId = cp.ProjectId,
                ProjectName = cp.Project?.Name ?? "",
                MentorName = cp.Project?.Mentor?.FullName ?? "",
                Result = cp.Result?.Name ?? "—",
                Place = cp.Place?.ToString() ?? "—"
            }).ToList();

            EditBtn.Visibility = CurrentSession.IsStudent ? Visibility.Collapsed : Visibility.Visible;
            DeleteBtn.Visibility = CurrentSession.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_contest == null) return;
            var dialog = new Dialogs.ContestDialog(_contest, true);
            if (dialog.ShowDialog() == true) await LoadAsync();
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_contest == null || !CurrentSession.IsAdmin) return;

            var result = MessageBox.Show(
                $"Удалить конкурс «{_contest.Name}»?\n\nВместе с конкурсом будут удалены все записи об участии проектов в нём.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var contest = await db.Contests.FirstOrDefaultAsync(c => c.Id == _contestId);
                if (contest == null)
                {
                    MessageBox.Show("Конкурс не найден. Возможно, он уже был удалён.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
                    return;
                }

                db.Contests.Remove(contest);
                await db.SaveChangesAsync();

                MessageBox.Show($"Конкурс «{contest.Name}» успешно удалён.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить конкурс:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProjectsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsGrid.SelectedItem is { } item)
            {
                var projectId = (int)item.GetType().GetProperty("ProjectId")!.GetValue(item)!;
                NavigationService?.Navigate(new ProjectDetailsPage(projectId));
            }
        }
    }
}