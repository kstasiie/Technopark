using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class ContestsPage : Page
    {
        private List<ContestViewModel> _allContests = [];
        private bool _searchFocused = false;

        public ContestsPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            await LoadFiltersAsync();
            await LoadContestsAsync();

            if (CurrentSession.IsStudent)
                AddContestBtn.Visibility = Visibility.Collapsed;

            if (!CurrentSession.IsAdmin)
                ActionsColumn.Visibility = Visibility.Collapsed;
        }

        private async Task LoadFiltersAsync()
        {
            using var db = new AppDbContext();
            var levels = await db.ContestLevels.ToListAsync();
            var all = new List<object> { new { Id = 0, Name = "Все уровни" } };
            all.AddRange(levels.Cast<object>());
            LevelFilter.ItemsSource = all;
            LevelFilter.DisplayMemberPath = "Name";
            LevelFilter.SelectedValuePath = "Id";
            LevelFilter.SelectedIndex = 0;
        }

        private async Task LoadContestsAsync()
        {
            using var db = new AppDbContext();
            var contests = await db.Contests
                .Include(c => c.Level)
                .Include(c => c.Participations)
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            _allContests = contests.Select(c => new ContestViewModel
            {
                Contest = c,
                Name = c.Name,
                Organizer = c.Organizer,
                Level = c.Level,
                Date = c.Date,
                DateDisplay = c.DateDisplay,
                LevelId = c.LevelId,
                ParticipationCount = c.Participations.Count
            }).ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allContests == null || ContestsGrid == null) return;

            var filtered = _allContests.AsEnumerable();

            var levelId = (int?)LevelFilter.SelectedValue;
            if (levelId.HasValue && levelId > 0)
                filtered = filtered.Where(c => c.LevelId == levelId);

            // Поиск
            if (!_isPlaceholder && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var q = SearchBox.Text.ToLower();
                filtered = filtered.Where(p => p.Name.ToLower().Contains(q));
            }

            ContestsGrid.ItemsSource = filtered.ToList();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
            => ApplyFilter();

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

        private async void AddContestBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.ContestDialog();
            if (dialog.ShowDialog() == true)
                await LoadContestsAsync();
        }

        private async void ContestsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ContestsGrid.SelectedItem is ContestViewModel vm)
            {
                bool canEdit = !CurrentSession.IsStudent;
                var dialog = new Dialogs.ContestDialog(vm.Contest, canEdit);
                if (dialog.ShowDialog() == true)
                    await LoadContestsAsync();
            }
        }
        private async void EditContest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ContestViewModel vm)
            {
                bool canEdit = !CurrentSession.IsStudent;
                var dialog = new Dialogs.ContestDialog(vm.Contest, canEdit);
                if (dialog.ShowDialog() == true)
                    await LoadContestsAsync();
            }
        }

        private async void DeleteContest_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (sender is not Button btn || btn.Tag is not ContestViewModel vm) return;
            if (!CurrentSession.IsAdmin) return;

            var result = MessageBox.Show(
                $"Удалить конкурс «{vm.Name}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var contest = await db.Contests.FindAsync(vm.Contest.Id);
            if (contest != null)
            {
                db.Contests.Remove(contest);
                await db.SaveChangesAsync();
                await LoadContestsAsync();
            }
        }
        private void ContestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContestsGrid.SelectedItem is ContestViewModel vm)
                NavigationService?.Navigate(new ContestDetailsPage(vm.Contest.Id));
        }
    }

    public class ContestViewModel
    {
        public Contest Contest { get; set; } = null!;
        public string Name { get; set; } = "";
        public string Organizer { get; set; } = "";
        public ContestLevel? Level { get; set; }
        public int LevelId { get; set; }
        public DateTime Date { get; set; }
        public string DateDisplay { get; set; } = "";
        public int ParticipationCount { get; set; }
    }
}