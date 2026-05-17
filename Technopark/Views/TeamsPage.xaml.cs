using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class TeamsPage : Page
    {
        private List<TeamViewModel> _allTeams = [];
        private bool _searchFocused = false;

        public TeamsPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadTeamsAsync();

            if (CurrentSession.IsStudent)
                AddTeamBtn.Visibility = Visibility.Collapsed;
            if (!CurrentSession.IsAdmin)
                ActionsColumn.Visibility = Visibility.Collapsed;
        }

        private async Task LoadTeamsAsync()
        {
            using var db = new AppDbContext();
            var teams = await db.Teams
                .Include(t => t.Members)
                .Include(t => t.Projects)
                .OrderBy(t => t.Name)
                .ToListAsync();

            _allTeams = teams.Select(t => new TeamViewModel
            {
                TeamId = t.Id,
                Name = t.Name,
                FormationYear = t.FormationYear,
                MemberCount = t.Members.Count,
                ProjectCount = t.Projects.Count
            }).ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allTeams == null || TeamsGrid == null) return;

            var filtered = _allTeams.AsEnumerable();

            if (!_isPlaceholder && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var q = SearchBox.Text.ToLower();
                filtered = filtered.Where(p => p.Name.ToLower().Contains(q));
            }

            TeamsGrid.ItemsSource = filtered.ToList();
        }

        private bool _isPlaceholder = true;

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_isPlaceholder)
            {
                _isPlaceholder = false;  // ← СНАЧАЛА флаг
                SearchBox.Text = "";
                SearchBox.Foreground = System.Windows.Media.Brushes.Black;
            }
            _searchFocused = true;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                _isPlaceholder = true;  // ← СНАЧАЛА флаг
                SearchBox.Text = "Поиск по названию...";
                SearchBox.Foreground = System.Windows.Media.Brushes.Gray;
                _searchFocused = false;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isPlaceholder) ApplyFilter();
        }

        private async void AddTeamBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.TeamDialog();
            if (dialog.ShowDialog() == true)
                await LoadTeamsAsync();
        }

        private async void EditTeam_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (sender is not Button btn || btn.Tag is not int teamId) return;

            var team = await db.Teams
                .Include(t => t.Members).ThenInclude(m => m.Student)
                .Include(t => t.Members).ThenInclude(m => m.Role)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null) return;

            bool canEdit = !CurrentSession.IsStudent;
            var dialog = new Dialogs.TeamDialog(team, canEdit);
            if (dialog.ShowDialog() == true)
                await LoadTeamsAsync();
        }

        private async void DeleteTeam_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (sender is not Button btn || btn.Tag is not int teamId) return;
            if (!CurrentSession.IsAdmin) return;

            var team = await db.Teams.FindAsync(teamId);
            if (team == null) return;

            var result = MessageBox.Show(
                $"Удалить команду «{team.Name}»?\nВсе связанные проекты потеряют привязку к команде.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            db.Teams.Remove(team);
            await db.SaveChangesAsync();
            await LoadTeamsAsync();
        }
        private void TeamsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeamsGrid.SelectedItem is TeamViewModel vm)
                NavigationService?.Navigate(new TeamDetailsPage(vm.TeamId));
        }

    }

    public class TeamViewModel
    {
        public int TeamId { get; set; }
        public string Name { get; set; } = "";
        public int FormationYear { get; set; }
        public int MemberCount { get; set; }
        public int ProjectCount { get; set; }
    }
}