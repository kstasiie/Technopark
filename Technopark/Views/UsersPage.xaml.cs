using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class UsersPage : Page
    {
        private readonly AppDbContext _db = new();
        private List<User> _allUsers = [];
        private bool _searchFocused = false;

        public UsersPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            _allUsers = await _db.Users
                .Include(u => u.MentorProfile)
                .Include(u => u.StudentProfile)
                .OrderBy(u => u.Login)
                .ToListAsync();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allUsers == null || UsersGrid == null) return;

            var filtered = _allUsers.AsEnumerable();

            var roleItem = RoleFilter.SelectedItem as ComboBoxItem;
            var role = roleItem?.Content?.ToString();
            filtered = role switch
            {
                "Администратор" => filtered.Where(u => u.Role == "Admin"),
                "Наставник" => filtered.Where(u => u.Role == "Mentor"),
                "Участник" => filtered.Where(u => u.Role == "Student"),
                _ => filtered
            };

            // Сначала конвертируем в ViewModel
            var viewModels = filtered.Select(u => new UserViewModel
            {
                UserId = u.Id,
                Login = u.Login,
                Role = u.Role,
                IsActive = u.IsActive,
                DisplayName = u.Role == "Mentor"
                    ? u.MentorProfile?.FullName ?? u.Login
                    : u.Role == "Student"
                        ? u.StudentProfile?.FullName ?? u.Login
                        : "Администратор",
                PositionOrClass = u.Role == "Mentor"
                    ? u.MentorProfile?.Position ?? ""
                    : "",
                DirectionName = u.Role == "Mentor"
                    ? u.MentorProfile?.Direction?.Name ?? ""
                    : ""
            }).ToList();

            // Поиск по ViewModel
            if (!_isPlaceholder && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var q = SearchBox.Text.ToLower();
                viewModels = viewModels.Where(u =>
                    u.DisplayName.ToLower().Contains(q) ||
                    u.Login.ToLower().Contains(q)).ToList();
            }

            UsersGrid.ItemsSource = viewModels;
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

        private void RoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void AddUserBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.UserDialog();
            if (dialog.ShowDialog() == true)
                _ = LoadUsersAsync();
        }

        private void UsersGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UsersGrid.SelectedItem is User user)
            {
                var dialog = new Dialogs.UserDialog(user);
                if (dialog.ShowDialog() == true)
                    _ = LoadUsersAsync();
            }
        }
        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int userId) return;

            var user = await _db.Users
                .Include(u => u.MentorProfile)
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return;

            var dialog = new Dialogs.UserDialog(user);
            if (dialog.ShowDialog() == true)
                await LoadUsersAsync();
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int userId) return;

            if (userId == CurrentSession.UserId)
            {
                MessageBox.Show("Нельзя удалить текущего пользователя");
                return;
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;

            var result = MessageBox.Show(
                $"Удалить пользователя «{user.Login}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            await LoadUsersAsync();
        }
        private async void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not UserViewModel vm) return;

            if (vm.Role == "Mentor")
            {
                var mentor = await _db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == vm.UserId);
                if (mentor != null)
                    NavigationService?.Navigate(new MentorDetailsPage(mentor.Id));
            }
            else if (vm.Role == "Student")
            {
                var student = await _db.StudentProfiles
                    .FirstOrDefaultAsync(s => s.UserId == vm.UserId);
                if (student != null)
                    NavigationService?.Navigate(new StudentDetailsPage(student.Id));
            }
        }

    }
}