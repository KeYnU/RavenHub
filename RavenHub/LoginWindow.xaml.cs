using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;

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
            this.Loaded += Window_Loaded; // Добавляем обработчик загрузки окна
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем фокус на поле логина при загрузке
            txtUsername.Focus();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            await AttemptLogin();
        }

        private async void btnLogin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AttemptLogin();
            }
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptLogin().ConfigureAwait(false);
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
                    Dispatcher.Invoke(() =>
                    {
                        var mainWindow = new MainWindow
                        {
                            Username = username,
                            IsAdmin = isAdmin
                        };
                        Application.Current.MainWindow = mainWindow;
                        this.Close();
                        mainWindow.Show();
                    });
                }
                else
                {
                    ShowError("Неверный логин или пароль");
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
            var shakeAnimation = (Storyboard)FindResource("ShakeAnimation");
            shakeAnimation.Completed += (s, e) => _isAnimating = false;
            shakeAnimation.Begin(MainBorder);

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
                ((PackIcon)btnTogglePassword.Content).Kind = PackIconKind.EyeOffOutline;
                btnTogglePassword.ToolTip = "Скрыть пароль";
                txtPasswordVisible.Focus();
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                ((PackIcon)btnTogglePassword.Content).Kind = PackIconKind.EyeOutline;
                btnTogglePassword.ToolTip = "Показать пароль";
                txtPassword.Focus();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void DragWindow(object sender, MouseButtonEventArgs e) => DragMove();

        protected override void OnClosed(EventArgs e)
        {
            _userRepository?.Dispose();
            base.OnClosed(e);
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {          
            var registerWindow = new RegisterWindow();
            this.Hide();

            registerWindow.ShowDialog();

            this.Show();
            txtUsername.Focus();
        }
    }
}