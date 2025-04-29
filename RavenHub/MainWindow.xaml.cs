using System.Windows;
using RavenHub.Pages;

namespace RavenHub
{
    public partial class MainWindow : Window
    {
        private readonly SidebarManager _sidebarManager;

        public MainWindow()
        {
            InitializeComponent();
            _sidebarManager = new SidebarManager(MainGrid, NavPanel, ButtonsPanel);
            Loaded += OnLoaded;
            MainFrame.Navigate(new HomePage());
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            StatisticsButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            _sidebarManager.InitializeTextBlocks();
            _sidebarManager.CompleteInitialization();
        }

        public bool IsAdmin { get; set; }
        public string Username { get; set; }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new HomePage());

        private void EquipmentButton_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new EquipmentPage());

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new EmployeesPage());

        private void TasksButton_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new TasksPage());

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new StatisticsPage());

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
            => new Pages.Settings.SettingsWindow { Owner = this }.ShowDialog();

        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            _sidebarManager.ToggleSidebar();
        }
    }
}