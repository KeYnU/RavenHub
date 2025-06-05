using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace RavenHub
{
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow(BitmapImage image)
        {
            InitializeComponent();
            FullSizeImage.Source = image;

            // Закрытие по клику на затемненную область
            OverlayBackground.MouseDown += (s, e) => Close();

            // Закрытие по Esc
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}