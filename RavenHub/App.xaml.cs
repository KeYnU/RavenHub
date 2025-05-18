using System;
using System.Windows;
using System.Configuration;
using System.Windows.Media.Animation;

namespace RavenHub
{
    public partial class App : Application
    {
        private SplashWindow _splashWindow;
        public static DatabaseConnectionService DbService { get; private set; }

        // Этот метод будет вызываться из App.xaml через Startup="App_OnStartup"
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            // Инициализация сервиса подключения
            var connectionString = ConfigurationManager.ConnectionStrings["RavenHubDB"]?.ConnectionString
                    ?? @"Data Source=.;Initial Catalog=RavenHub;Integrated Security=True;";
            DbService = new DatabaseConnectionService(connectionString);

            // Обработчик необработанных исключений
            this.DispatcherUnhandledException += (exceptionSender, ex) =>
            {
                Logger.Log($"Необработанная ошибка: {ex.Exception}", LogLevel.Error);
                MessageBox.Show("Критическая ошибка. Подробности в логах.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            ShowSplashScreen();
        }

        private async void ShowSplashScreen()
        {
            _splashWindow = new SplashWindow();
            _splashWindow.Show();

            bool isConnected = await _splashWindow.CheckConnectionWithFeedback(DbService);

            if (!isConnected)
            {
                MessageBox.Show(
                    "Не удалось подключиться к базе данных.\n\n" +
                    "Приложение будет работать в ограниченном режиме.",
                    "Ошибка подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            FadeOutSplash();
        }

        private void FadeOutSplash()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            fadeOut.Completed += (s, e) =>
            {
                _splashWindow.Close();
                ShowLoginWindow();
            };

            _splashWindow.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void ShowLoginWindow()
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Current.MainWindow = loginWindow;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DbService?.Dispose();
            base.OnExit(e);
        }
    }
}