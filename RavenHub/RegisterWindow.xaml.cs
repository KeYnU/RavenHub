using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace RavenHub
{
    public partial class RegisterWindow : Window
    {
        private readonly UserRepository _userRepository;

        public RegisterWindow()
        {
            InitializeComponent();
            _userRepository = new UserRepository(App.DbService);

            // Инициализация обработчиков событий
            RegisterButton.Click += RegisterButton_Click;
            CancelButton.Click += CancelButton_Click;
            btnClose.Click += btnClose_Click;
            btnMinimize.Click += btnMinimize_Click;

            // Анимация плавного появления
            Opacity = 0;
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            BeginAnimation(OpacityProperty, fadeInAnimation);

            Loaded += (sender, e) => txtUsername.Focus();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            // Валидация
            if (login.Length < 4)
            {
                MessageBox.Show("Логин должен содержать минимум 4 символа", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (password.Length < 8)
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (await _userRepository.UserExistsAsync(login))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (await _userRepository.RegisterUserAsync(login, password))
                {
                    MessageBox.Show("Регистрация успешно завершена", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void DragWindow(object sender, MouseButtonEventArgs e) => DragMove();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _userRepository.Dispose();
        }
    }
}