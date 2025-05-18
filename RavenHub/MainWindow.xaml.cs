using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using RavenHub.Pages;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;

namespace RavenHub
{
    public partial class MainWindow : Window
    {
        private readonly SidebarManager _sidebarManager;
        private bool _isClosing;

        public string Username { get; set; }
        public bool IsAdmin { get; set; }

        public MainWindow()
        {
            try
            {
                Logger.Log("[MainWindow] Конструктор начал выполнение", LogLevel.Debug);
                InitializeComponent();
                Logger.Log("[MainWindow] Инициализация компонентов завершена", LogLevel.Debug);

                _sidebarManager = new SidebarManager(MainGrid, NavPanel, ButtonsPanel);
                Logger.Log("[MainWindow] SidebarManager инициализирован", LogLevel.Debug);

                this.Closing += MainWindow_Closing;
                this.Closed += MainWindow_Closed;

                Loaded += OnLoaded;
                MainFrame.Navigated += OnFrameNavigated;

                Logger.Log("[MainWindow] Конструктор завершил выполнение", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Критическая ошибка в конструкторе");
                MessageBox.Show("Не удалось инициализировать главное окно. Приложение будет закрыто.",
                    "Фатальная ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosing) return;
            Logger.Log("[MainWindow] Начало закрытия окна (Closing)", LogLevel.Info);
            e.Cancel = true; // Блокируем стандартное закрытие
            SafeClose();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Logger.Log("[MainWindow] Окно закрыто (Closed)", LogLevel.Info);
            CleanupResources();

            if (Application.Current.Windows.Count == 0)
            {
                Logger.Log("[MainWindow] Это последнее окно - завершаем приложение", LogLevel.Info);
                Application.Current.Shutdown();
            }
        }

        private void SafeClose()
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                Logger.Log("[MainWindow] Безопасное закрытие окна", LogLevel.Debug);
                this.Dispatcher.BeginInvoke(new Action(() => this.Close()));
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при безопасном закрытии");
                _isClosing = false;
            }
        }

        private void CleanupResources()
        {
            try
            {
                Logger.Log("[MainWindow] Очистка ресурсов", LogLevel.Debug);

                Loaded -= OnLoaded;
                MainFrame.Navigated -= OnFrameNavigated;
                Closing -= MainWindow_Closing;
                Closed -= MainWindow_Closed;

                if (MainFrame.Content is IDisposable disposable)
                {
                    disposable.Dispose();
                    Logger.Log("[MainWindow] Ресурсы страницы освобождены", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при очистке ресурсов");
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Log($"[MainWindow] Окно загружено. Пользователь: {Username ?? "null"}, Admin: {IsAdmin}", LogLevel.Info);

                StatisticsButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
                Logger.Log($"[MainWindow] Статистика доступна: {IsAdmin}", LogLevel.Debug);

                _sidebarManager.InitializeTextBlocks();
                _sidebarManager.CompleteInitialization();
                Logger.Log("[MainWindow] Сайдбар инициализирован", LogLevel.Debug);

                Logger.Log("[MainWindow] Начало первичной навигации", LogLevel.Debug);
                MainFrame.Navigate(new HomePage(MainFrame, Username));
                Logger.Log("[MainWindow] Навигация на HomePage выполнена", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка в обработчике OnLoaded");
                MessageBox.Show("Произошла ошибка при загрузке главного окна", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                Logger.Log($"[MainWindow] Навигация на страницу: {e.Content?.GetType().Name ?? "null"}", LogLevel.Debug);

                if (e.Content is IDisposable disposablePage)
                {
                    disposablePage.Dispose();
                    Logger.Log("[MainWindow] Ресурсы страницы освобождены", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка в обработчике OnFrameNavigated");
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка 'Главная'", LogLevel.Debug);
            try
            {
                MainFrame.Navigate(new HomePage(MainFrame, Username));
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при переходе на HomePage");
            }
        }

        private void EquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка 'Оборудование'", LogLevel.Debug);
            try
            {
                MainFrame.Navigate(new EquipmentPage());
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при переходе на EquipmentPage");
            }
        }

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка 'Сотрудники'", LogLevel.Debug);
            try
            {
                MainFrame.Navigate(new EmployeesPage(MainFrame, Username, IsAdmin));
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при переходе на EmployeesPage");
            }
        }

        private void TasksButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка 'Задачи'", LogLevel.Debug);
            try
            {
                MainFrame.Navigate(new TasksPage());
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при переходе на TasksPage");
            }
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка 'Статистика'", LogLevel.Debug);
            try
            {
                MainFrame.Navigate(new StatisticsPage());
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при переходе на StatisticsPage");
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка 'Настройки'", LogLevel.Debug);
            try
            {
                new Pages.Settings.SettingsWindow { Owner = this }.ShowDialog();
                Logger.Log("[MainWindow] Окно настроек закрыто", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при открытии SettingsWindow");
            }
        }

        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка переключения сайдбара", LogLevel.Debug);
            try
            {
                _sidebarManager.ToggleSidebar();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при переключении сайдбара");
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Начало выхода из системы", LogLevel.Info);
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение выхода",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Logger.Log("[MainWindow] Пользователь подтвердил выход", LogLevel.Info);

                    var loginWindow = new LoginWindow();
                    loginWindow.Show();

                    Logger.Log("[MainWindow] Окно входа показано", LogLevel.Debug);

                    SafeClose();
                    Logger.Log("[MainWindow] Текущее окно закрыто", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при выходе из системы");
            }
        }
    }
}