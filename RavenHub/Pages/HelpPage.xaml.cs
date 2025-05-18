using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace RavenHub.Pages
{
    public partial class HelpPage : Page
    {
        public HelpPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Стандартный способ навигации назад
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
            else
            {
                // Альтернатива: возврат на HomePage
                NavigationService?.Navigate(new HomePage(
                    ((MainWindow)Application.Current.MainWindow).MainFrame,
                    ((MainWindow)Application.Current.MainWindow).Username));
            }
        }
    }
}