using ControlzEx.Theming;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;

namespace RavenHub.Helpers
{
    public static class ThemeManager
    {
        private static readonly PaletteHelper _paletteHelper = new PaletteHelper();

        public static void ApplyTheme(bool isDarkTheme)
        {
            var theme = _paletteHelper.GetTheme();
            theme.SetBaseTheme(isDarkTheme ? BaseTheme.Dark : BaseTheme.Light);
            _paletteHelper.SetTheme(theme);

            // Применяем дополнительные стили для темной темы
            if (isDarkTheme)
            {
                try
                {
                    var darkThemeUri = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
                    var darkThemeDict = new ResourceDictionary { Source = darkThemeUri };

                    // Добавляем в самый конец, чтобы переопределить все остальное
                    Application.Current.Resources.MergedDictionaries.Add(darkThemeDict);

                    Logger.Log("[ThemeManager] Темная тема применена", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[ThemeManager] Ошибка загрузки темной темы: {ex.Message}", LogLevel.Error);
                }
            }
            else
            {
                // Удаляем темную тему
                var darkThemeDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.ToString().Contains("DarkTheme.xaml") == true);

                if (darkThemeDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(darkThemeDict);
                }
            }

            // ОБНОВЛЯЕМ ЗАГОЛОВКИ ВСЕХ ОТКРЫТЫХ ОКОН
            UpdateAllWindowTitles();
        }

        private static void UpdateAllWindowTitles()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    try
                    {
                        WindowThemeHelper.ApplyWindowTheme(window);
                        Logger.Log($"[ThemeManager] Обновлен заголовок окна: {window.GetType().Name}", LogLevel.Debug);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[ThemeManager] Ошибка обновления заголовка окна {window.GetType().Name}: {ex.Message}", LogLevel.Error);
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        public static bool IsWindowsInDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    if (value != null)
                    {
                        return (int)value == 0;
                    }
                }
            }
            catch
            {
                // По умолчанию светлая тема
            }
            return false;
        }

        public static bool GetCurrentThemeIsDark()
        {
            var theme = _paletteHelper.GetTheme();
            return theme.GetBaseTheme() == BaseTheme.Dark;
        }

        public static void InitializeTheme()
        {
            bool isDarkMode = IsWindowsInDarkMode();
            ApplyTheme(isDarkMode);
        }

        public static void StartThemeWatcher()
        {
            SystemEvents.UserPreferenceChanged += (sender, args) =>
            {
                if (args.Category == UserPreferenceCategory.General)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        bool isDarkMode = IsWindowsInDarkMode();
                        ApplyTheme(isDarkMode);
                    });
                }
            };
        }

        public static void ToggleTheme()
        {
            bool isDark = GetCurrentThemeIsDark();
            ApplyTheme(!isDark);
        }
    }
}