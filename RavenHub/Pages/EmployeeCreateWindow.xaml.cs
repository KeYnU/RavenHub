using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace RavenHub.Pages
{
    public partial class EmployeeCreateWindow : Window
    {
        public ObservableCollection<string> Positions { get; }
        public Dictionary<string, string> EmployeeData { get; private set; }

        public EmployeeCreateWindow(ObservableCollection<string> positions)
        {
            InitializeComponent();
            Positions = positions;
            DataContext = this;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            EmployeeData = new Dictionary<string, string>
            {
                { "FullName", FullNameTextBox.Text ?? "" },
                { "Position", PositionComboBox.SelectedItem?.ToString() ?? "" },
                { "PhoneNumber", PhoneNumberTextBox.Text ?? "" },
                { "Email", EmailTextBox.Text ?? "" },
                { "SocialLink", SocialLinkTextBox.Text ?? "" }
            };

            if (string.IsNullOrWhiteSpace(EmployeeData["FullName"]) || string.IsNullOrWhiteSpace(EmployeeData["Position"]))
            {
                MessageBox.Show("Пожалуйста, заполните ФИО и выберите должность.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}