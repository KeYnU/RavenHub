using System.Windows;
using System.Windows.Controls;

namespace RavenHub.Pages.Settings
{
    public partial class SettingsWindow : Window
    {
        private readonly LocaleManager _locale = LocaleManager.Instance; // Глобальный экземпляр

        public SettingsWindow()
        {
            InitializeComponent(); // Эта строка должна быть первой!
            this.DataContext = _locale;
            LanguageComboBox.SelectedIndex = 0; // Выбираем русский по умолчанию
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                string lang = item.Tag.ToString();
                _locale.SetLanguage(lang); // Применяем язык глобально
                MessageBox.Show($"Успешно");
            }
        }
    }
}