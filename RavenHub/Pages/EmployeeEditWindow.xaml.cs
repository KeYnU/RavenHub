using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace RavenHub.Pages
{
    public partial class EmployeeEditWindow : Window
    {
        public ObservableCollection<string> Positions { get; }
        public Dictionary<string, string> EmployeeData { get; private set; }

        public EmployeeEditWindow(Dictionary<string, string> employeeData, ObservableCollection<string> positions)
        {
            InitializeComponent();
            Positions = positions;
            EmployeeData = employeeData;

            FullNameTextBox.Text = employeeData["FullName"];
            PositionComboBox.SelectedItem = employeeData["Position"];
            PhoneNumberTextBox.Text = employeeData["PhoneNumber"];
            EmailTextBox.Text = employeeData["Email"];
            SocialLinkTextBox.Text = employeeData["SocialLink"];

            DataContext = this;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            EmployeeData = new Dictionary<string, string>
            {
                { "Id", EmployeeData["Id"] },
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