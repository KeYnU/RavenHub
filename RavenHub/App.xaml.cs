using System;
using System.Threading;
using System.Windows;
using Microsoft.VisualBasic;

namespace RavenHub
{
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            
            // Для Лого
            var splash = new SplashScreen("Start/splash.png");
            splash.Show(false);
            Thread.Sleep(2500);
            splash.Close(TimeSpan.Zero);

            // Открываем основное окно
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.MainWindow = loginWindow;
        }
    }
}