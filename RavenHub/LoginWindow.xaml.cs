using MaterialDesignThemes.Wpf;
using RavenHub.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RavenHub
{
    public partial class LoginWindow : Window
    {
        private readonly UserRepository _userRepository;
        private bool _isAnimating;
        private bool _passwordVisible;

        public LoginWindow()
        {
            InitializeComponent();
            _userRepository = new UserRepository(App.DbService);
            _passwordVisible = false;
            UpdatePasswordVisibility();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();
            // Применяем тему окна используя ваш WindowThemeHelper
            WindowThemeHelper.ApplyWindowTheme(this);
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            await AttemptLogin();
        }

        private async void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AttemptLogin();
            }
        }

        private async Task AttemptLogin()
        {
            if (_isAnimating) return;

            string username = txtUsername.Text?.Trim() ?? "";
            string password = _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            try
            {
                var (isAuthenticated, isAdmin) = await _userRepository.VerifyUserAsync(username, password);

                if (isAuthenticated)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // Проверяем, нет ли уже открытого MainWindow
                        var existingMainWindow = Application.Current.Windows
                            .OfType<MainWindow>()
                            .FirstOrDefault();

                        if (existingMainWindow != null)
                        {
                            // Если MainWindow уже существует, активируем его
                            existingMainWindow.Activate();
                            existingMainWindow.WindowState = WindowState.Normal;
                        }
                        else
                        {
                            // Создаем новое главное окно только если его нет
                            var mainWindow = new MainWindow
                            {
                                Username = username,
                                IsAdmin = isAdmin
                            };

                            Application.Current.MainWindow = mainWindow;
                            mainWindow.Show();
                        }

                        // Закрываем окно входа
                        this.Close();
                    });
                }
                else
                {
                    ShowError("Неверный логин или пароль");
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Ошибка входа");
                ShowError("Ошибка сервера");
            }
        }

        private void ShowError(string message)
        {
            if (_isAnimating) return;

            _isAnimating = true;

            // Запускаем анимацию встряски
            var shakeAnimation = (Storyboard)FindResource("ShakeAnimation");
            shakeAnimation.Completed += (s, e) =>
            {
                _isAnimating = false;
                // Сбрасываем позицию после анимации
                if (LoginCard.RenderTransform is TranslateTransform transform)
                {
                    transform.X = 0;
                }
            };
            shakeAnimation.Begin();

            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible;
            UpdatePasswordVisibility();
        }

        private void UpdatePasswordVisibility()
        {
            if (_passwordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                eyeIcon.Kind = PackIconKind.EyeOffOutline;
                btnTogglePassword.ToolTip = "Скрыть пароль";
                txtPasswordVisible.Focus();
                txtPasswordVisible.SelectionStart = txtPasswordVisible.Text.Length;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                eyeIcon.Kind = PackIconKind.EyeOutline;
                btnTogglePassword.ToolTip = "Показать пароль";
                txtPassword.Focus();
            }
        }

        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Анимация исчезновения
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            fadeOut.Completed += (s, args) =>
            {
                this.Hide();

                var registerWindow = new RegisterWindow();
                registerWindow.Owner = this;
                registerWindow.WindowStartupLocation = WindowStartupLocation.Manual;

                // Позиционируем окно регистрации в том же месте
                registerWindow.Left = this.Left;
                registerWindow.Top = this.Top;

                bool? result = registerWindow.ShowDialog();

                if (result == true)
                {
                    // Если регистрация успешна и был выполнен автовход
                    this.Close();
                }
                else
                {
                    // Возвращаемся к окну входа
                    this.Show();

                    // Анимация появления
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                    this.BeginAnimation(OpacityProperty, fadeIn);

                    txtUsername.Focus();
                }
            };

            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        protected override void OnClosed(EventArgs e)
        {
            _userRepository?.Dispose();
            base.OnClosed(e);

            // Завершаем приложение только если нет главного окна
            if (Application.Current.MainWindow == null || Application.Current.MainWindow == this)
            {
                Application.Current.Shutdown();
            }
        }
    }
}