using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Views
{
    public partial class MentorDetailsPage : Page
    {
        private readonly int _mentorId;
        private MentorProfile? _mentor;

        public MentorDetailsPage(int mentorId)
        {
            InitializeComponent();
            _mentorId = mentorId;
            Loaded += async (s, e) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            using var db = new AppDbContext();
            _mentor = await db.MentorProfiles
                .Include(m => m.Direction)
                .Include(m => m.User)
                .Include(m => m.Projects).ThenInclude(p => p.Direction)
                .Include(m => m.Projects).ThenInclude(p => p.Status)
                .FirstOrDefaultAsync(m => m.Id == _mentorId);

            if (_mentor == null) return;

            FullNameText.Text = _mentor.FullName;
            PositionText.Text = $"Должность: {_mentor.Position}";
            DirectionText.Text = $"Направление: {_mentor.Direction?.Name}";
            AvatarText.Text = _mentor.LastName.Length > 0 && _mentor.FirstName.Length > 0
                ? $"{_mentor.LastName[0]}{_mentor.FirstName[0]}" : "?";

            ProjectsGrid.ItemsSource = _mentor.Projects.ToList();

            // Контакты — только Admin
            if (CurrentSession.IsAdmin)
            {
                EmailText.Text = $"Email: {_mentor.Email ?? "не указан"}";
                PhoneText.Text = $"Телефон: {_mentor.Phone ?? "не указан"}";
            }
            else
            {
                ContactsBlock.Visibility = Visibility.Collapsed;
            }

            // Кнопки только для Admin
            bool isOwnProfile = _mentor.UserId == CurrentSession.UserId;
            EditBtn.Visibility = (CurrentSession.IsAdmin || isOwnProfile)
                ? Visibility.Visible : Visibility.Collapsed;
            DeleteBtn.Visibility = (CurrentSession.IsAdmin && !isOwnProfile)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            if (_mentor?.User == null) return;
            var user = await db.Users
                .Include(u => u.MentorProfile)
                .FirstOrDefaultAsync(u => u.Id == _mentor.UserId);
            if (user == null) return;
            var dialog = new Dialogs.UserDialog(user);
            if (dialog.ShowDialog() == true) await LoadAsync();
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_mentor == null || !CurrentSession.IsAdmin) return;

            if (_mentor.UserId == CurrentSession.UserId)
            {
                MessageBox.Show("Нельзя удалить собственный профиль.",
                    "Действие запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить наставника «{_mentor.FullName}»?\n\nВместе с наставником будут удалены его учётная запись и все проекты, которые он ведёт.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var user = await db.Users.FindAsync(_mentor.UserId);
                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден. Возможно, наставник уже удалён.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
                    return;
                }

                db.Users.Remove(user);
                await db.SaveChangesAsync();

                MessageBox.Show($"Наставник «{_mentor.FullName}» успешно удалён.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить наставника:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProjectsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsGrid.SelectedItem is Project p)
                NavigationService?.Navigate(new ProjectDetailsPage(p.Id));
        }
    }
}