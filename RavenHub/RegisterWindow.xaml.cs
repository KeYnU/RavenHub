using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace RavenHub
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();

            // Устанавливаем начальную прозрачность окна в 0
            Opacity = 0;

            // Создаём анимацию появления
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5) // Анимация длится 0.5 секунды
            };

            // Применяем анимацию к свойству Opacity
            BeginAnimation(OpacityProperty, fadeInAnimation);

            // Фокус на поле ввода логина при открытии
            Loaded += (sender, e) => txtUsername.Focus();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            // Проверка длины логина
            if (login.Length < 4)
            {
                MessageBox.Show("Логин должен содержать минимум 4 символа.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка длины пароля
            if (password.Length < 8)
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var userRepository = new UserRepository();
            if (userRepository.UserExists(login))
            {
                MessageBox.Show("Пользователь с таким логином уже существует.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            userRepository.RegisterUser(login, password, false);
            MessageBox.Show("Пользователь успешно зарегистрирован.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}