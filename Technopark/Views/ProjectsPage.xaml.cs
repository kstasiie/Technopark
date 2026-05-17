using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class ProjectsPage : Page
    {
        private List<Project> _allProjects = [];
        private bool _searchFocused = false;
        private readonly bool _onlyMy;

        public ProjectsPage(bool onlyMyProjects = false)
        {
            InitializeComponent();
            _onlyMy = onlyMyProjects;
            Loaded += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            await LoadFiltersAsync();
            await LoadProjectsAsync();

            // Скрываем кнопку добавления для участника
            if (CurrentSession.IsStudent)
                AddProjectBtn.Visibility = Visibility.Collapsed;

            if (!CurrentSession.IsAdmin)
                ActionsColumn.Visibility = Visibility.Collapsed;
        }

        private async Task LoadFiltersAsync()
        {
            using var db = new AppDbContext();
            var directions = await db.Directions.OrderBy(d => d.Name).ToListAsync();
            var allDir = new List<object> { new { Id = 0, Name = "Все направления" } };
            allDir.AddRange(directions.Cast<object>());
            DirectionFilter.ItemsSource = allDir;
            DirectionFilter.DisplayMemberPath = "Name";
            DirectionFilter.SelectedValuePath = "Id";
            DirectionFilter.SelectedIndex = 0;

            var statuses = await db.ProjectStatuses.ToListAsync();
            var allSt = new List<object> { new { Id = 0, Name = "Все статусы" } };
            allSt.AddRange(statuses.Cast<object>());
            StatusFilter.ItemsSource = allSt;
            StatusFilter.DisplayMemberPath = "Name";
            StatusFilter.SelectedValuePath = "Id";
            StatusFilter.SelectedIndex = 0;
        }

        private async Task LoadProjectsAsync()
        {
            using var db = new AppDbContext();
            var query = db.Projects
                .Include(p => p.Direction)
                .Include(p => p.Mentor)
                .Include(p => p.Team)
                .Include(p => p.Status)
                .AsQueryable();

            if (_onlyMy && CurrentSession.IsMentor)
            {
                var mentor = await db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == CurrentSession.UserId);
                if (mentor != null)
                    query = query.Where(p => p.MentorId == mentor.Id);
            }

            _allProjects = await query
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allProjects == null || ProjectsGrid == null) return;
            var filtered = _allProjects.AsEnumerable();

            var dirId = (int?)DirectionFilter.SelectedValue;
            if (dirId.HasValue && dirId > 0)
                filtered = filtered.Where(p => p.DirectionId == dirId);

            var stId = (int?)StatusFilter.SelectedValue;
            if (stId.HasValue && stId > 0)
                filtered = filtered.Where(p => p.StatusId == stId);

            if (!_isPlaceholder && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var q = SearchBox.Text.ToLower();
                filtered = filtered.Where(p => p.Name.ToLower().Contains(q));
            }

            ProjectsGrid.ItemsSource = filtered.ToList();
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

        private async void AddProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.ProjectDialog();
            if (dialog.ShowDialog() == true)
                await LoadProjectsAsync();
        }

        private void ProjectsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsGrid.SelectedItem is Project project)
                NavigationService?.Navigate(new ProjectDetailsPage(project.Id));
        }
        private async void EditProject_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (sender is Button btn && btn.Tag is Project project)
            {
                var full = await db.Projects
                    .Include(p => p.Direction).Include(p => p.Status)
                    .Include(p => p.Mentor).Include(p => p.Team)
                    .FirstOrDefaultAsync(p => p.Id == project.Id);

                if (full == null) return;

                var mentor = await db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == CurrentSession.UserId);
                bool canEdit = CurrentSession.IsAdmin ||
                    (CurrentSession.IsMentor && full.MentorId == mentor?.Id);

                var dialog = new Dialogs.ProjectDialog(full, canEdit);
                if (dialog.ShowDialog() == true)
                    await LoadProjectsAsync();
            }
        }

        private async void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (sender is not Button btn || btn.Tag is not Project project) return;
            if (!CurrentSession.IsAdmin) return;

            var result = MessageBox.Show(
                $"Удалить проект «{project.Name}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var full = await db.Projects.FindAsync(project.Id);
            if (full != null)
            {
                db.Projects.Remove(full);
                await db.SaveChangesAsync();
                await LoadProjectsAsync();
            }
        }
    }
}