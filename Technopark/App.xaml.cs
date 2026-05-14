using System.Windows;
using System.Windows.Threading;

namespace Technopark
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show(
                    $"Ошибка:\n\n{ex.Exception.Message}\n\n{ex.Exception.InnerException?.Message}\n\nStackTrace:\n{ex.Exception.StackTrace}",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            try
            {
                var login = new LoginWindow();
                login.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка");
                Shutdown();
            }
        }
    }
}