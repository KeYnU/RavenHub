using System.Collections.Generic;
using System.Windows;

namespace RavenHub.Pages
{
    public partial class CreateTaskWindow : Window
    {
        public Dictionary<string, string> TaskData { get; private set; }

        public CreateTaskWindow()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название задачи");
                return;
            }

            TaskData = new Dictionary<string, string>
            {
                { "Title", TitleTextBox.Text },
                { "Description", DescriptionTextBox.Text ?? "" }
            };

            DialogResult = true;
            Close();
        }
    }
}