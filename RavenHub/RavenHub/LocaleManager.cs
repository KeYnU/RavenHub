using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;

namespace RavenHub
{
    public class LocaleManager : INotifyPropertyChanged
    {
        private static LocaleManager _instance;
        public static LocaleManager Instance => _instance ?? (_instance = new LocaleManager());

        private string _currentLanguage = "ru";
        private Dictionary<string, Dictionary<string, string>> _translations;

        public LocaleManager()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["ru"] = new Dictionary<string, string>
                {
                    {"Home", "Главная"},
                    {"Equipment", "Оборудование"},
                    {"Employees", "Сотрудники"},
                    {"Tasks", "Задачи"},
                    {"Statistics", "Статистика"},
                    {"Settings", "Настройки"},
                    {"LanguageLabel", "Язык"}
                },
                ["en"] = new Dictionary<string, string>
                {
                    {"Home", "Home"},
                    {"Equipment", "Equipment"},
                    {"Employees", "Employees"},
                    {"Tasks", "Tasks"},
                    {"Statistics", "Statistics"},
                    {"Settings", "Settings"},
                    {"LanguageLabel", "Language"}
                }
            };
        }

        public string Home => _translations[_currentLanguage]["Home"];
        public string Equipment => _translations[_currentLanguage]["Equipment"];
        public string Employees => _translations[_currentLanguage]["Employees"];
        public string Tasks => _translations[_currentLanguage]["Tasks"];
        public string Statistics => _translations[_currentLanguage]["Statistics"];
        public string Settings => _translations[_currentLanguage]["Settings"];
        public string LanguageLabel => _translations[_currentLanguage]["LanguageLabel"];

        public string this[string key] => _translations[_currentLanguage].TryGetValue(key, out var value) ? value : $"#{key}#";

        public void SetLanguage(string languageCode)
        {
            if (_currentLanguage != languageCode)
            {
                _currentLanguage = languageCode;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Home)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Equipment)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Employees)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tasks)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Statistics)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Settings)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageLabel)));
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}