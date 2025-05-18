using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RavenHub
{
    public partial class SplashWindow : Window
    {
        public System.Windows.Controls.ProgressBar ProgressBar => ProgressBarControl;

        public SplashWindow()
        {
            InitializeComponent();
        }

        public async Task<bool> CheckConnectionWithFeedback(DatabaseConnectionService dbService)
        {
            // Первая попытка
            UpdateStatus("Подключение к базе данных...", Colors.White);
            ProgressBarControl.Value = 30;

            var (isConnected, error) = await dbService.TestConnectionAsync();

            if (!isConnected)
            {
                // Вторая попытка с задержкой
                UpdateStatus("Повторная попытка подключения...", Colors.Orange);
                ProgressBarControl.Value = 60;
                await Task.Delay(2000);

                (isConnected, error) = await dbService.TestConnectionAsync();
                ProgressBarControl.Value = 90;
            }

            if (isConnected)
            {
                UpdateStatus("✓ Подключение успешно", Colors.LimeGreen);
                ProgressBarControl.Value = 100;
                ProgressBarControl.Foreground = new SolidColorBrush(Colors.LimeGreen);
                await Task.Delay(1000);
                return true;
            }
            else
            {
                UpdateStatus("✗ Не удалось подключиться", Colors.Red);
                ProgressBarControl.Foreground = new SolidColorBrush(Colors.Red);
                await Task.Delay(1500);
                return false;
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionStatusText.Text = message;
                ConnectionStatusText.Foreground = new SolidColorBrush(color);

                // Создаем новую анимацию вместо поиска в ресурсах
                var storyboard = new Storyboard();
                var animation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    AutoReverse = false
                };

                Storyboard.SetTarget(animation, ConnectionStatusText);
                Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(animation);
                storyboard.Begin();
            });
        }
    }
}