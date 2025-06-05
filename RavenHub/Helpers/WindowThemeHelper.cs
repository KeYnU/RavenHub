    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using MaterialDesignThemes.Wpf;

    namespace RavenHub.Helpers
    {
        public static class WindowThemeHelper
        {
            [DllImport("dwmapi.dll")]
            private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

            private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            private const int DWMWA_CAPTION_COLOR = 35;

            public static void ApplyWindowTheme(Window window, Color? backgroundColor = null)
            {
                try
                {
                    var helper = new WindowInteropHelper(window);
                    IntPtr hwnd = helper.Handle;

                    if (!backgroundColor.HasValue)
                    {
                        // Получаем цвет из Material Design темы
                        backgroundColor = GetMaterialDesignBackgroundColor();
                    }

                    Color color = backgroundColor.Value;
                    bool isDarkBackground = IsDarkColor(color);

                    int darkMode = isDarkBackground ? 1 : 0;
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                    int colorValue = ColorToInt(color);
                    DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colorValue, sizeof(int));
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, "[WindowThemeHelper] Ошибка применения темы окна");
                }
            }

            private static Color GetMaterialDesignBackgroundColor()
            {
                try
                {
                    // Сначала пытаемся получить цвет из ресурсов приложения
                    var backgroundBrush = Application.Current.TryFindResource("MaterialDesignBackground") as SolidColorBrush;
                    if (backgroundBrush != null)
                    {
                        return backgroundBrush.Color;
                    }

                    // Если не нашли, используем стандартный подход
                    var paletteHelper = new PaletteHelper();
                    var theme = paletteHelper.GetTheme();
                    return theme.Background;
                }
                catch
                {
                    // Если не удалось получить цвет, возвращаем белый
                    return Colors.White;
                }
            }

            private static Color GetWindowBackgroundColor(Window window)
            {
                Color backgroundColor = GetMaterialDesignBackgroundColor();

                if (window.Background is SolidColorBrush windowBrush)
                {
                    backgroundColor = windowBrush.Color;
                }
                else if (window.Content is FrameworkElement element)
                {
                    if (element is System.Windows.Controls.Grid grid && grid.Background is SolidColorBrush gridBrush)
                    {
                        backgroundColor = gridBrush.Color;
                    }
                }

                return backgroundColor;
            }

            private static bool IsDarkColor(Color color)
            {
                double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                return brightness < 0.5;
            }

            private static int ColorToInt(Color color)
            {
                return (color.B << 16) | (color.G << 8) | color.R;
            }
        }
    }