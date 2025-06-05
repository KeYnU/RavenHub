using MaterialDesignThemes.Wpf;
using RavenHub.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace RavenHub
{
    public partial class RegisterWindow : Window
    {
        private readonly UserRepository _userRepository;
        private bool _passwordVisible;

        public RegisterWindow()
        {
            InitializeComponent();
            _userRepository = new UserRepository(App.DbService);
            _passwordVisible = false;
            UpdatePasswordVisibility();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();
            WindowThemeHelper.ApplyWindowTheme(this);

            // Анимация появления
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            this.BeginAnimation(OpacityProperty, fadeIn);
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            await AttemptRegister();
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptRegister().ConfigureAwait(false);
            }
        }

        private async Task AttemptRegister()
        {
            string username = txtUsername.Text.Trim();
            string password = _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;
            string confirmPassword = txtPasswordConfirm.Password;

            // Валидация
            if (username.Length < 4)
            {
                ShowError("Логин должен содержать минимум 4 символа");
                txtUsername.Focus();
                return;
            }

            if (password.Length < 8)
            {
                ShowError("Пароль должен содержать минимум 8 символов");
                txtPassword.Focus();
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают");
                txtPasswordConfirm.Focus();
                return;
            }

            try
            {
                RegisterButton.IsEnabled = false;
                RegisterButton.Content = "Регистрация...";

                if (await _userRepository.UserExistsAsync(username))
                {
                    ShowError("Пользователь с таким логином уже существует");
                    txtUsername.Focus();
                    return;
                }

                if (await _userRepository.RegisterUserAsync(username, password))
                {
                    MessageBox.Show("Регистрация успешно завершена!\nТеперь вы можете войти в систему.",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Автоматический вход после регистрации
                    await AutoLogin(username, password);
                }
                else
                {
                    ShowError("Ошибка при регистрации. Попробуйте позже.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Ошибка регистрации");
                ShowError($"Ошибка регистрации: {ex.Message}");
            }
            finally
            {
                RegisterButton.IsEnabled = true;
                RegisterButton.Content = "Register";
            }
        }

        private async Task AutoLogin(string username, string password)
        {
            try
            {
                var (isAuthenticated, isAdmin) = await _userRepository.VerifyUserAsync(username, password);

                if (isAuthenticated)
                {
                    // Анимация исчезновения перед переходом к главному окну
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                    fadeOut.Completed += async (s, args) =>
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            var existingMainWindow = Application.Current.Windows
                                .OfType<MainWindow>()
                                .FirstOrDefault();

                            if (existingMainWindow == null)
                            {
                                var mainWindow = new MainWindow
                                {
                                    Username = username,
                                    IsAdmin = isAdmin
                                };

                                Application.Current.MainWindow = mainWindow;
                                mainWindow.Show();
                            }

                            this.DialogResult = true;
                            this.Close();
                        });
                    };

                    this.BeginAnimation(OpacityProperty, fadeOut);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Ошибка автоматического входа после регистрации");
                this.DialogResult = true;
                this.Close();
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Анимация исчезновения
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            fadeOut.Completed += (s, args) =>
            {
                this.DialogResult = false;
                this.Close();
            };

            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        protected override void OnClosed(EventArgs e)
        {
            _userRepository?.Dispose();
            base.OnClosed(e);
        }
    }
}