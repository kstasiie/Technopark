using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class TeamDetailsPage : Page
    {
        private readonly int _teamId;
        private Team? _team;

        public TeamDetailsPage(int teamId)
        {
            InitializeComponent();
            _teamId = teamId;
            Loaded += async (s, e) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            using var db = new AppDbContext();
            _team = await db.Teams
                .Include(t => t.Members).ThenInclude(m => m.Student)
                .Include(t => t.Members).ThenInclude(m => m.Role)
                .Include(t => t.Projects).ThenInclude(p => p.Direction)
                .Include(t => t.Projects).ThenInclude(p => p.Status)
                .FirstOrDefaultAsync(t => t.Id == _teamId);

            if (_team == null) return;

            NameText.Text = _team.Name;
            YearText.Text = $"Год формирования: {_team.FormationYear}";

            MembersList.ItemsSource = _team.Members.Select(m => new
            {
                Id = m.StudentId,
                Name = m.Student?.FullName ?? "",
                Role = m.Role?.Name ?? ""
            }).ToList();

            ProjectsGrid.ItemsSource = _team.Projects.ToList();

            EditBtn.Visibility = CurrentSession.IsStudent ? Visibility.Collapsed : Visibility.Visible;
            DeleteBtn.Visibility = CurrentSession.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_team == null) return;
            var dialog = new Dialogs.TeamDialog(_team, true);
            if (dialog.ShowDialog() == true) await LoadAsync();
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_team == null || !CurrentSession.IsAdmin) return;

            var result = MessageBox.Show(
                $"Удалить команду «{_team.Name}»?\n\nВместе с командой будут удалены все её проекты и связи с участниками.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == _teamId);
                if (team == null)
                {
                    MessageBox.Show("Команда не найдена. Возможно, она уже была удалена.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
                    return;
                }

                db.Teams.Remove(team);
                await db.SaveChangesAsync();

                MessageBox.Show($"Команда «{team.Name}» успешно удалена.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить команду:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MemberLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.Tag is int studentId)
                NavigationService?.Navigate(new StudentDetailsPage(studentId));
        }

        private void ProjectsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsGrid.SelectedItem is Project p)
                NavigationService?.Navigate(new ProjectDetailsPage(p.Id));
        }
    }
}