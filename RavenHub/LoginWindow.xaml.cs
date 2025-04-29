using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;

namespace RavenHub
{
    public partial class LoginWindow : Window
    {
        private readonly UserRepository _userRepository;

        public LoginWindow()
        {
            InitializeComponent();
            _userRepository = new UserRepository();

            txtPassword.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter) AttemptLogin();
            };

            txtPasswordVisible.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter) AttemptLogin();
            };

            Loaded += (sender, e) => txtUsername.Focus();
        }

        private void AttemptLogin()
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Visibility == Visibility.Visible
                ? txtPassword.Password
                : txtPasswordVisible.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var (isAuthenticated, isAdmin) = _userRepository.VerifyUser(username, password);
            if (isAuthenticated)
            {
                new MainWindow
                {
                    IsAdmin = isAdmin,
                    Username = username
                }.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TogglePasswordVisibility(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            if (txtPasswordVisible.Visibility == Visibility.Visible)
            {
                var hideAnimation = (Storyboard)FindResource("HidePasswordAnimation");
                hideAnimation.Completed += (s, _) =>
                {
                    txtPasswordVisible.Visibility = Visibility.Collapsed;
                    txtPassword.Visibility = Visibility.Visible;
                    txtPassword.Password = txtPasswordVisible.Text;

                    var showAnimation = (Storyboard)FindResource("ShowPasswordAnimation");
                    showAnimation.Begin(txtPassword);
                };
                hideAnimation.Begin(txtPasswordVisible);
            }
            else
            {
                var hideAnimation = (Storyboard)FindResource("HidePasswordAnimation");
                hideAnimation.Completed += (s, _) =>
                {
                    txtPassword.Visibility = Visibility.Collapsed;
                    txtPasswordVisible.Visibility = Visibility.Visible;
                    txtPasswordVisible.Text = txtPassword.Password;

                    // Анимация появления текстового поля
                    var showAnimation = (Storyboard)FindResource("ShowPasswordAnimation");
                    showAnimation.Begin(txtPasswordVisible);
                };
                hideAnimation.Begin(txtPassword);
            }

            // Меняем иконку и подсказку
            var icon = (PackIcon)button.Content;
            icon.Kind = icon.Kind == PackIconKind.EyeOutline
                ? PackIconKind.EyeOffOutline
                : PackIconKind.EyeOutline;

            button.ToolTip = icon.Kind == PackIconKind.EyeOutline
                ? "Show password"
                : "Hide password";

            // Фокусируем активное поле
            await System.Threading.Tasks.Task.Delay(200); // Ждем завершения анимации
            if (txtPasswordVisible.Visibility == Visibility.Visible)
                txtPasswordVisible.Focus();
            else
                txtPassword.Focus();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow { Owner = this }.ShowDialog();
        }
    }
}