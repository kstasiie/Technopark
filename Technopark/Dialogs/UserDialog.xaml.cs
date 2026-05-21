using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Technopark.Data;
using Technopark.Models;
using Technopark.Services;

namespace Technopark.Dialogs
{
    public partial class UserDialog : Window
    {
        private readonly int? _editUserId;

        public UserDialog()
        {
            InitializeComponent();
            _ = LoadAsync(null);
        }

        public UserDialog(User user)
        {
            InitializeComponent();
            _editUserId = user.Id;
            TitleText.Text = "Редактировать пользователя";
            PasswordLabel.Text = "Новый пароль (оставьте пустым чтобы не менять)";
            _ = LoadAsync(user.Id);
        }

        private async Task LoadAsync(int? userId)
        {
            using var db = new AppDbContext();

            var directions = await db.Directions.OrderBy(d => d.Name).ToListAsync();
            DirectionBox.ItemsSource = directions;
            DirectionBox.DisplayMemberPath = "Name";
            DirectionBox.SelectedValuePath = "Id";

            if (userId == null) return;

            var user = await db.Users
                .Include(u => u.MentorProfile)
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return;

            LoginBox.Text = user.Login;
            foreach (ComboBoxItem item in RoleBox.Items)
                if (item.Tag?.ToString() == user.Role)
                    item.IsSelected = true;

            if (user.Role == "Mentor" && user.MentorProfile != null)
            {
                LastNameBox.Text = user.MentorProfile.LastName;
                FirstNameBox.Text = user.MentorProfile.FirstName;
                MiddleNameBox.Text = user.MentorProfile.MiddleName ?? "";
                PositionBox.Text = user.MentorProfile.Position;
                EmailBox.Text = user.MentorProfile.Email ?? "";
                PhoneBox.Text = user.MentorProfile.Phone ?? "";
                DirectionBox.SelectedValue = user.MentorProfile.DirectionId;
            }
            else if (user.Role == "Student" && user.StudentProfile != null)
            {
                LastNameBox.Text = user.StudentProfile.LastName;
                FirstNameBox.Text = user.StudentProfile.FirstName;
                MiddleNameBox.Text = user.StudentProfile.MiddleName ?? "";
                EmailBox.Text = user.StudentProfile.Email ?? "";
                PhoneBox.Text = user.StudentProfile.Phone ?? "";
                if (user.StudentProfile.BirthDate.HasValue)
                    BirthDateBox.Text = user.StudentProfile.BirthDate.Value
                        .ToString("dd.MM.yyyy");
            }

            // Защита от смены роли
            bool isOwnProfile = user.Id == CurrentSession.UserId;
            bool canChangeRole = CurrentSession.IsAdmin && !isOwnProfile;
            if (!canChangeRole)
            {
                RoleBox.IsEnabled = false;
                RoleBox.ToolTip = isOwnProfile
                    ? "Нельзя изменить собственную роль"
                    : "Только администратор может менять роли";
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LastNameBox.Text) ||
                string.IsNullOrWhiteSpace(FirstNameBox.Text) ||
                string.IsNullOrWhiteSpace(LoginBox.Text))
            {
                MessageBox.Show("Заполните обязательные поля: Фамилия, Имя, Логин.",
                    "Не заполнены поля", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var role = (RoleBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Student";

            // Защита от смены собственной роли (двойная защита — UI уже блокирует)
            if (_editUserId == CurrentSession.UserId && CurrentSession.IsAdmin
                && role != CurrentSession.Role)
            {
                MessageBox.Show("Нельзя изменить собственную роль.",
                    "Действие запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new AppDbContext();
                bool isNew = _editUserId == null;

                if (isNew)
                {
                    if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        MessageBox.Show("Введите пароль для нового пользователя.",
                            "Не заполнено поле", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Проверка уникальности логина
                    if (await db.Users.AnyAsync(u => u.Login == LoginBox.Text.Trim()))
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует. Выберите другой логин.",
                            "Логин занят", MessageBoxButton.OK, MessageBoxImage.Warning);
                        LoginBox.Focus();
                        return;
                    }

                    var salt = AuthService.GenerateSaltPublic();
                    var user = new User
                    {
                        Login = LoginBox.Text.Trim(),
                        Salt = salt,
                        PasswordHash = AuthService.HashPasswordPublic(PasswordBox.Password, salt),
                        Role = role,
                        IsActive = true
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync();

                    await CreateProfileAsync(db, user.Id, role);
                    await db.SaveChangesAsync();
                }
                else
                {
                    var user = await db.Users
                        .Include(u => u.MentorProfile)
                        .Include(u => u.StudentProfile)
                        .FirstOrDefaultAsync(u => u.Id == _editUserId);

                    if (user == null)
                    {
                        MessageBox.Show("Пользователь не найден. Возможно, он был удалён.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    // Проверка уникальности логина при изменении
                    var newLogin = LoginBox.Text.Trim();
                    if (user.Login != newLogin &&
                        await db.Users.AnyAsync(u => u.Login == newLogin))
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует. Выберите другой логин.",
                            "Логин занят", MessageBoxButton.OK, MessageBoxImage.Warning);
                        LoginBox.Focus();
                        return;
                    }

                    user.Login = newLogin;

                    bool isOwnProfile = user.Id == CurrentSession.UserId;
                    bool canChangeRole = CurrentSession.IsAdmin && !isOwnProfile;
                    if (canChangeRole)
                        user.Role = role;

                    if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        var salt = AuthService.GenerateSaltPublic();
                        user.Salt = salt;
                        user.PasswordHash = AuthService.HashPasswordPublic(PasswordBox.Password, salt);
                    }

                    await UpdateProfileAsync(db, user);
                    await db.SaveChangesAsync();
                }

                MessageBox.Show(
                    isNew ? "Пользователь успешно создан." : "Данные пользователя успешно обновлены.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить пользователя:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CreateProfileAsync(AppDbContext db, int userId, string role)
        {
            if (role == "Mentor")
            {
                db.MentorProfiles.Add(new MentorProfile
                {
                    UserId = userId,
                    LastName = LastNameBox.Text.Trim(),
                    FirstName = FirstNameBox.Text.Trim(),
                    MiddleName = MiddleNameBox.Text.Trim(),
                    Position = PositionBox.Text.Trim(),
                    DirectionId = (int)(DirectionBox.SelectedValue ?? 1),
                    Email = EmailBox.Text.Trim(),
                    Phone = PhoneBox.Text.Trim()
                });
            }
            else if (role == "Student")
            {
                DateTime.TryParseExact(BirthDateBox.Text, "dd.MM.yyyy",
                    null, System.Globalization.DateTimeStyles.None, out var birthDate);
                db.StudentProfiles.Add(new StudentProfile
                {
                    UserId = userId,
                    LastName = LastNameBox.Text.Trim(),
                    FirstName = FirstNameBox.Text.Trim(),
                    MiddleName = MiddleNameBox.Text.Trim(),
                    BirthDate = birthDate == default ? null : birthDate,
                    Email = EmailBox.Text.Trim(),
                    Phone = PhoneBox.Text.Trim()
                });
            }
            await Task.CompletedTask;
        }

        private async Task UpdateProfileAsync(AppDbContext db, User user)
        {
            if (user.Role == "Mentor")
            {
                var profile = user.MentorProfile
                    ?? new MentorProfile { UserId = user.Id };

                profile.LastName = LastNameBox.Text.Trim();
                profile.FirstName = FirstNameBox.Text.Trim();
                profile.MiddleName = MiddleNameBox.Text.Trim();
                profile.Position = PositionBox.Text.Trim();
                profile.DirectionId = (int)(DirectionBox.SelectedValue ?? 1);
                profile.Email = EmailBox.Text.Trim();
                profile.Phone = PhoneBox.Text.Trim();

                if (user.MentorProfile == null) db.MentorProfiles.Add(profile);
            }
            else if (user.Role == "Student")
            {
                var profile = user.StudentProfile
                    ?? new StudentProfile { UserId = user.Id };

                profile.LastName = LastNameBox.Text.Trim();
                profile.FirstName = FirstNameBox.Text.Trim();
                profile.MiddleName = MiddleNameBox.Text.Trim();
                profile.Email = EmailBox.Text.Trim();
                profile.Phone = PhoneBox.Text.Trim();

                DateTime.TryParseExact(BirthDateBox.Text, "dd.MM.yyyy",
                    null, System.Globalization.DateTimeStyles.None, out var bd);
                profile.BirthDate = bd == default ? null : bd;

                if (user.StudentProfile == null) db.StudentProfiles.Add(profile);
            }
            await Task.CompletedTask;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}