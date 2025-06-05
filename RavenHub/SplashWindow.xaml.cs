using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace RavenHub
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
            StartAnimations();
        }

        private void StartAnimations()
        {
            // Анимация прыгающих точек
            var bounceDuration = TimeSpan.FromSeconds(0.8);
            var easing = new ElasticEase { Oscillations = 1, Springiness = 3 };

            // Анимация для первой точки
            var bounce1 = new DoubleAnimation(0, -15, bounceDuration)
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = easing
            };
            Dot1.RenderTransform.BeginAnimation(TranslateTransform.YProperty, bounce1);

            // Анимация для второй точки (с задержкой)
            var bounce2 = new DoubleAnimation(0, -15, bounceDuration)
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = easing,
                BeginTime = TimeSpan.FromSeconds(0.15)
            };
            Dot2.RenderTransform.BeginAnimation(TranslateTransform.YProperty, bounce2);

            // Анимация для третьей точки (с задержкой)
            var bounce3 = new DoubleAnimation(0, -15, bounceDuration)
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = easing,
                BeginTime = TimeSpan.FromSeconds(0.3)
            };
            Dot3.RenderTransform.BeginAnimation(TranslateTransform.YProperty, bounce3);

            // Пульсация логотипа
            var pulse = new DoubleAnimation(0.9, 1.05, TimeSpan.FromSeconds(2))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            var scale = new ScaleTransform();
            LogoText.RenderTransform = scale;
            LogoText.RenderTransformOrigin = new Point(0.5, 0.5);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        public async Task<bool> CheckDatabaseConnection(DatabaseConnectionService dbService)
        {
            // Первая попытка
            UpdateStatus("Установка соединения с базой данных...", Color.FromRgb(110, 195, 255));

            var (isConnected, error) = await dbService.TestConnectionAsync();

            if (!isConnected)
            {
                // Вторая попытка с задержкой
                UpdateStatus("Повторное подключение...", Color.FromRgb(255, 180, 0));
                await Task.Delay(2000);

                (isConnected, error) = await dbService.TestConnectionAsync();
            }

            if (isConnected)
            {
                UpdateStatus("✓ Соединение установлено", Color.FromRgb(100, 220, 100));
                await Task.Delay(1000);
                return true;
            }
            else
            {
                UpdateStatus($"✗ Ошибка: {error}", Color.FromRgb(255, 100, 100));
                await Task.Delay(1500);
                return false;
            }
        }

        public void UpdateStatus(string message, Color color)
        {
            Dispatcher.Invoke(() =>
            {
                // Анимация исчезновения
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                fadeOut.Completed += (s, e) =>
                {
                    ConnectionStatusText.Text = message;
                    ConnectionStatusText.Foreground = new SolidColorBrush(color);

                    // Анимация появления
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                    ConnectionStatusText.BeginAnimation(OpacityProperty, fadeIn);
                };

                ConnectionStatusText.BeginAnimation(OpacityProperty, fadeOut);
            });
        }
    }
}