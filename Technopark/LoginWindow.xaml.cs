using System.Windows;
using Technopark.Data;
using Technopark.Services;

namespace Technopark
{
    public partial class LoginWindow : Window
    {
        private AuthService? _authService;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var db = new AppDbContext();
                _authService = new AuthService(db);
                await _authService.EnsureAdminExistsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных:\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_authService == null)
            {
                MessageBox.Show("Нет подключения к базе данных");
                return;
            }

            ErrorText.Visibility = Visibility.Collapsed;
            LoginBtn.IsEnabled = false;

            var login = LoginBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Введите логин и пароль";
                ErrorText.Visibility = Visibility.Visible;
                LoginBtn.IsEnabled = true;
                return;
            }

            var user = await _authService.LoginAsync(login, password);

            if (user == null)
            {
                ErrorText.Text = "Неверный логин или пароль";
                ErrorText.Visibility = Visibility.Visible;
                LoginBtn.IsEnabled = true;
                return;
            }

            CurrentSession.UserId = user.Id;
            CurrentSession.Login = user.Login;
            CurrentSession.Role = user.Role;

            // Получаем имя из профиля
            CurrentSession.FullName = user.Role switch
            {
                "Mentor" => user.MentorProfile?.FullName ?? user.Login,
                "Student" => user.StudentProfile?.FullName ?? user.Login,
                _ => "Администратор"
            };

            var main = new MainWindow();
            main.Show();
            Close();
        }
    }
}