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
        private readonly AppDbContext _db = new();
        private readonly User? _editUser;

        public UserDialog()
        {
            InitializeComponent();
            _ = LoadDirectionsAsync();
        }

        public UserDialog(User user)
        {
            InitializeComponent();
            _editUser = user;
            TitleText.Text = "Редактировать пользователя";
            PasswordLabel.Text = "Новый пароль (оставьте пустым чтобы не менять)";
            _ = LoadDirectionsAsync(user);
        }

        private async Task LoadDirectionsAsync(User? user = null)
        {
            var directions = await _db.Directions.OrderBy(d => d.Name).ToListAsync();
            DirectionBox.ItemsSource = directions;
            DirectionBox.DisplayMemberPath = "Name";
            DirectionBox.SelectedValuePath = "Id";

            if (user != null)
            {
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
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LastNameBox.Text) ||
                string.IsNullOrWhiteSpace(FirstNameBox.Text) ||
                string.IsNullOrWhiteSpace(LoginBox.Text))
            {
                MessageBox.Show("Заполните обязательные поля (Фамилия, Имя, Логин)");
                return;
            }

            var role = (RoleBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Student";

            if (_editUser == null)
            {
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    MessageBox.Show("Введите пароль");
                    return;
                }

                var salt = AuthService.GenerateSaltPublic();
                var user = new User
                {
                    Login = LoginBox.Text.Trim(),
                    Salt = salt,
                    PasswordHash = AuthService.HashPasswordPublic(
                        PasswordBox.Password, salt),
                    Role = role,
                    IsActive = true
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                await CreateProfileAsync(user);
            }
            else
            {
                _editUser.Login = LoginBox.Text.Trim();
                _editUser.Role = role;

                if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    var salt = AuthService.GenerateSaltPublic();
                    _editUser.Salt = salt;
                    _editUser.PasswordHash = AuthService.HashPasswordPublic(
                        PasswordBox.Password, salt);
                }

                await UpdateProfileAsync(_editUser);
                _db.Users.Update(_editUser);
                await _db.SaveChangesAsync();
            }

            DialogResult = true;
            Close();
        }

        private async Task CreateProfileAsync(User user)
        {
            if (user.Role == "Mentor")
            {
                var profile = new MentorProfile
                {
                    UserId = user.Id,
                    LastName = LastNameBox.Text.Trim(),
                    FirstName = FirstNameBox.Text.Trim(),
                    MiddleName = MiddleNameBox.Text.Trim(),
                    Position = PositionBox.Text.Trim(),
                    DirectionId = (int)(DirectionBox.SelectedValue ?? 1),
                    Email = EmailBox.Text.Trim(),
                    Phone = PhoneBox.Text.Trim()
                };
                _db.MentorProfiles.Add(profile);
            }
            else if (user.Role == "Student")
            {
                DateTime.TryParseExact(BirthDateBox.Text, "dd.MM.yyyy",
                    null, System.Globalization.DateTimeStyles.None, out var birthDate);
                var profile = new StudentProfile
                {
                    UserId = user.Id,
                    LastName = LastNameBox.Text.Trim(),
                    FirstName = FirstNameBox.Text.Trim(),
                    MiddleName = MiddleNameBox.Text.Trim(),
                    BirthDate = birthDate == default ? null : birthDate,
                    Email = EmailBox.Text.Trim(),
                    Phone = PhoneBox.Text.Trim()
                };
                _db.StudentProfiles.Add(profile);
            }
            await _db.SaveChangesAsync();
        }

        private async Task UpdateProfileAsync(User user)
        {
            if (user.Role == "Mentor")
            {
                var profile = await _db.MentorProfiles
                    .FirstOrDefaultAsync(m => m.UserId == user.Id);
                if (profile != null)
                {
                    profile.LastName = LastNameBox.Text.Trim();
                    profile.FirstName = FirstNameBox.Text.Trim();
                    profile.MiddleName = MiddleNameBox.Text.Trim();
                    profile.Position = PositionBox.Text.Trim();
                    profile.DirectionId = (int)(DirectionBox.SelectedValue ?? 1);
                    profile.Email = EmailBox.Text.Trim();
                    profile.Phone = PhoneBox.Text.Trim();
                }
            }
            else if (user.Role == "Student")
            {
                var profile = await _db.StudentProfiles
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (profile != null)
                {
                    profile.LastName = LastNameBox.Text.Trim();
                    profile.FirstName = FirstNameBox.Text.Trim();
                    profile.MiddleName = MiddleNameBox.Text.Trim();
                    profile.Email = EmailBox.Text.Trim();
                    profile.Phone = PhoneBox.Text.Trim();
                    DateTime.TryParseExact(BirthDateBox.Text, "dd.MM.yyyy",
                        null, System.Globalization.DateTimeStyles.None, out var bd);
                    profile.BirthDate = bd == default ? null : bd;
                }
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}