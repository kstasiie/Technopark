using System.Windows;
using System.Windows.Controls;
using Technopark.Services;
using Technopark.Views;

namespace Technopark
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetupForRole();
            MainFrame.Navigated += (s, ev) =>
            {
                BackButton.Visibility = MainFrame.CanGoBack
                    ? Visibility.Visible : Visibility.Collapsed;

                // Автоматически обновляем заголовок по типу страницы
                if (MainFrame.Content is Page page)
                {
                    PageTitle.Text = page switch
                    {
                        Views.DashboardPage => "Главная",
                        Views.ProjectsPage => "Проекты",
                        Views.ContestsPage => "Конкурсы",
                        Views.UsersPage => "Пользователи",
                        Views.TeamsPage => "Команды",
                        Views.ExportPage => "Экспорт",
                        Views.PortfolioPage => "Моё портфолио",
                        Views.ProjectDetailsPage => "Информация о проекте",
                        Views.MentorDetailsPage => "Профиль наставника",
                        Views.StudentDetailsPage => "Профиль участника",
                        Views.TeamDetailsPage => "Информация о команде",
                        Views.ContestDetailsPage => "Информация о конкурсе",
                        _ => PageTitle.Text
                    };
                }
            };
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack) MainFrame.GoBack();
        }

        private void SetupForRole()
        {
            UserNameText.Text = CurrentSession.FullName;
            AvatarText.Text = GetInitials(CurrentSession.FullName);

            UserRoleText.Text = CurrentSession.Role switch
            {
                "Admin" => "Администратор",
                "Mentor" => "Наставник",
                "Student" => "Участник",
                _ => CurrentSession.Role
            };

            if (CurrentSession.IsAdmin)
            {
                SectionMyData.Visibility = Visibility.Collapsed;
                BtnMyProjects.Visibility = Visibility.Collapsed;
                SectionPortfolio.Visibility = Visibility.Collapsed;
                BtnPortfolio.Visibility = Visibility.Collapsed;
                // SectionExport и BtnExport видны для Admin
            }
            else if (CurrentSession.IsMentor)
            {
                SectionAdmin.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnAnalytics.Visibility = Visibility.Collapsed;
                SectionPortfolio.Visibility = Visibility.Collapsed;
                BtnPortfolio.Visibility = Visibility.Collapsed;
                // SectionExport и BtnExport видны для Mentor
            }
            else // Student
            {
                SectionMyData.Visibility = Visibility.Collapsed;
                BtnMyProjects.Visibility = Visibility.Collapsed;
                SectionExport.Visibility = Visibility.Collapsed;
                BtnExport.Visibility = Visibility.Collapsed;
                SectionAdmin.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnAnalytics.Visibility = Visibility.Collapsed;
                BtnTeams.Visibility = Visibility.Collapsed;
            }
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
                NavigateTo(tag);
        }

        private void NavigateTo(string page)
        {
            Page? pageView = page switch
            {
                "Dashboard" => new Views.DashboardPage(),
                "Projects" => new Views.ProjectsPage(),
                "MyProjects" => new Views.ProjectsPage(),
                "Contests" => new Views.ContestsPage(),
                "Teams" => new Views.TeamsPage(),
                "Users" => new Views.UsersPage(),
                "Export" => new Views.ExportPage(),
                "Portfolio" => new Views.PortfolioPage(),
                "Analytics" => new Views.AnalyticsPage(),
                _ => null
            };

            if (pageView != null)
            {
                MainFrame.Navigate(pageView);

                // Очищаем историю — страницы из меню всегда корневые
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    while (MainFrame.CanGoBack)
                        MainFrame.RemoveBackEntry();
                    BackButton.Visibility = Visibility.Collapsed;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Выйти из системы?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CurrentSession.Clear();
                new LoginWindow().Show();
                Close();
            }
        }

        private static string GetInitials(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}"
                : fullName.Length > 0 ? fullName[0].ToString() : "?";
        }
    }
}