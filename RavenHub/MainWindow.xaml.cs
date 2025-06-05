using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using RavenHub.Pages;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Windows.Media;
using RavenHub.Helpers;

namespace RavenHub
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly SidebarManager _sidebarManager;
        private bool _isClosing;
        private string connectionString = @"Data Source=.; Initial Catalog=RavenHub; Integrated Security=True;";
        private bool _hasUserAvatar;
        private bool _showDefaultAvatar;
        private ImageSource _userAvatarSource;

        public string Username { get; set; }
        public bool IsAdmin { get; set; }

        public bool HasUserAvatar
        {
            get { return _hasUserAvatar; }
            set
            {
                if (_hasUserAvatar != value)
                {
                    _hasUserAvatar = value;
                    ShowDefaultAvatar = !value; // Инвертируем значение
                    OnPropertyChanged(nameof(HasUserAvatar));
                }
            }
        }

        public bool ShowDefaultAvatar
        {
            get { return _showDefaultAvatar; }
            set
            {
                if (_showDefaultAvatar != value)
                {
                    _showDefaultAvatar = value;
                    OnPropertyChanged(nameof(ShowDefaultAvatar));
                }
            }
        }

        public ImageSource UserAvatarSource
        {
            get { return _userAvatarSource; }
            set
            {
                if (_userAvatarSource != value)
                {
                    _userAvatarSource = value;
                    OnPropertyChanged(nameof(UserAvatarSource));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

                // Устанавливаем DataContext для привязки данных
                DataContext = this;
                // Устанавливаем начальное значение для отображения стандартной иконки
                ShowDefaultAvatar = true;
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

        public void LoadUserAvatar()
        {
            try
            {
                Logger.Log("[MainWindow] Загрузка аватарки пользователя", LogLevel.Debug);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT AvatarImage FROM UserAvatars WHERE Login = @Login";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Login", Username);

                    object result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        byte[] imageData = (byte[])result;

                        BitmapImage avatarImage = new BitmapImage();
                        using (MemoryStream ms = new MemoryStream(imageData))
                        {
                            avatarImage.BeginInit();
                            avatarImage.CacheOption = BitmapCacheOption.OnLoad;
                            avatarImage.StreamSource = ms;
                            avatarImage.EndInit();
                        }

                        UserAvatarSource = avatarImage;
                        HasUserAvatar = true;
                        Logger.Log("[MainWindow] Аватарка пользователя загружена", LogLevel.Debug);
                    }
                    else
                    {
                        UserAvatarSource = null;
                        HasUserAvatar = false;
                        Logger.Log("[MainWindow] У пользователя нет аватарки", LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при загрузке аватарки");
                UserAvatarSource = null;
                HasUserAvatar = false;
            }
        }

        public void UpdateAvatar()
        {
            Logger.Log("[MainWindow] Обновление аватарки пользователя", LogLevel.Debug);
            LoadUserAvatar();
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

                // Применяем тему окна
                WindowThemeHelper.ApplyWindowTheme(this);

                StatisticsButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
                Logger.Log($"[MainWindow] Статистика доступна: {IsAdmin}", LogLevel.Debug);

                // Устанавливаем DataContext для текстовых блоков
                HomeText.DataContext = LocaleManager.Instance;
                EquipmentText.DataContext = LocaleManager.Instance;
                EmployeesText.DataContext = LocaleManager.Instance;
                TasksText.DataContext = LocaleManager.Instance;
                StatisticsText.DataContext = LocaleManager.Instance;

                _sidebarManager.InitializeTextBlocks();
                _sidebarManager.CompleteInitialization();
                Logger.Log("[MainWindow] Сайдбар инициализирован", LogLevel.Debug);

                // Загружаем аватарку пользователя
                LoadUserAvatar();

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

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка 'Мой аккаунт'", LogLevel.Debug);
            try
            {
                MainFrame.Navigate(new AccountPage(Username, IsAdmin));
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при переходе на AccountPage");
            }
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("[MainWindow] Нажата кнопка переключения темы", LogLevel.Debug);
            try
            {
                ThemeManager.ToggleTheme();
                Logger.Log("[MainWindow] Тема переключена", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "[MainWindow] Ошибка при переключении темы");
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

        private void ImageBrush_SuggestionChosen(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }
    }
}