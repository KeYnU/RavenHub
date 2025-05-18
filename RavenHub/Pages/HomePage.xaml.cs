using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace RavenHub.Pages
{
    public partial class HomePage : Page
    {
        private readonly Frame _mainFrame;

        public HomePage(Frame mainFrame, string userName)
        {
            InitializeComponent();
            _mainFrame = mainFrame;

            // Установка приветствия
            if (string.IsNullOrEmpty(userName))
                UserNameRun.Text = "Добро пожаловать!";
            else
                UserNameRun.Text = $"Добро пожаловать, {userName}!";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new HelpPage());
        }
    }
}