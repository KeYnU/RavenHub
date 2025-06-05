using System;
using System.Windows;
using System.Configuration;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using RavenHub.Helpers;

namespace RavenHub
{
    public partial class App : Application
    {
        private SplashWindow _splashWindow;
        public static DatabaseConnectionService DbService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Инициализация темы ПЕРЕД созданием окон
            ThemeManager.InitializeTheme();
            // Запускаем наблюдатель за изменениями темы Windows
            ThemeManager.StartThemeWatcher();

            // Вызываем основную логику
            App_OnStartup(null, e);
        }

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

            try
            {
                // Обновляем статус перед проверкой подключения
                _splashWindow.UpdateStatus("Инициализация приложения...", Colors.White);

                // Имитация загрузки других компонентов
                await Task.Delay(800);
                _splashWindow.UpdateStatus("Загрузка конфигурации...", Colors.White);
                await Task.Delay(600);

                // Проверка подключения к БД
                bool isConnected = await _splashWindow.CheckDatabaseConnection(DbService);

                if (!isConnected)
                {
                    _splashWindow.UpdateStatus("Ошибка подключения к БД", Colors.Red);
                    await Task.Delay(1500);

                    MessageBox.Show(
                        "Не удалось подключиться к базе данных.\n\n" +
                        "Приложение будет работать в ограниченном режиме.",
                        "Ошибка подключения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    _splashWindow.UpdateStatus("Завершение инициализации...", Colors.LimeGreen);
                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Ошибка инициализации: {ex}", LogLevel.Error);
                _splashWindow.UpdateStatus($"Ошибка: {ex.Message}", Colors.Red);
                await Task.Delay(2000);
            }
            finally
            {
                FadeOutSplash();
            }
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
            // Убираем установку MainWindow здесь - это может мешать закрытию
            // Current.MainWindow = loginWindow;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DbService?.Dispose();
            base.OnExit(e);
        }
    }
}