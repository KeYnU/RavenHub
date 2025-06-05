using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Data.SqlClient;

namespace RavenHub.Pages
{
    public partial class AccountPage : Page
    {
        public string Username { get; set; }
        public bool IsAdmin { get; set; }
        public string Role => IsAdmin ? "Администратор" : "Пользователь";
        public string RegistrationDate { get; set; } = "dd.mm.yyyy";
        public BitmapImage UserAvatar { get; set; }

        private string connectionString = @"Data Source=.; Initial Catalog=RavenHub; Integrated Security=True;";

        public AccountPage(string username, bool isAdmin)
        {
            InitializeComponent();
            Username = username;
            IsAdmin = isAdmin;
            LoadUserAvatar();
            DataContext = this;
        }

        private void LoadUserAvatar()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT AvatarImage FROM UserAvatars WHERE Login = @Login";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Login", Username);

                    object result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        byte[] imageData = (byte[])result;

                        UserAvatar = new BitmapImage();
                        using (MemoryStream ms = new MemoryStream(imageData))
                        {
                            UserAvatar.BeginInit();
                            UserAvatar.CacheOption = BitmapCacheOption.OnLoad;
                            UserAvatar.StreamSource = ms;
                            UserAvatar.EndInit();
                        }
                    }
                    else
                    {
                        UserAvatar = CreateDefaultAvatar();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке аватара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                UserAvatar = CreateDefaultAvatar();
            }
        }

        private BitmapImage CreateDefaultAvatar()
        {
            int width = 200;
            int height = 200;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 200;
                pixels[i + 1] = 200;
                pixels[i + 2] = 200;
                pixels[i + 3] = 255;
            }

            BitmapSource bitmapSource = BitmapSource.Create(
                width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);

            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream ms = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(ms);
                ms.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        private void UploadAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                Title = "Выберите изображение для аватара"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    byte[] imageData = File.ReadAllBytes(openFileDialog.FileName);
                    SaveAvatarToDatabase(imageData);

                    BitmapImage bitmap = new BitmapImage();
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                    }

                    UserAvatar = bitmap;
                    AvatarImage.ImageSource = UserAvatar;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAvatarToDatabase(byte[] imageData)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string checkQuery = "SELECT COUNT(*) FROM UserAvatars WHERE Login = @Login";
                    SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@Login", Username);

                    int count = (int)checkCommand.ExecuteScalar();

                    if (count > 0)
                    {
                        string updateQuery = "UPDATE UserAvatars SET AvatarImage = @AvatarImage, UploadedAt = GETDATE() WHERE Login = @Login";
                        SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                        updateCommand.Parameters.AddWithValue("@Login", Username);
                        updateCommand.Parameters.AddWithValue("@AvatarImage", imageData);
                        updateCommand.ExecuteNonQuery();
                    }
                    else
                    {
                        string insertQuery = "INSERT INTO UserAvatars (Login, AvatarImage, UploadedAt) VALUES (@Login, @AvatarImage, GETDATE())";
                        SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                        insertCommand.Parameters.AddWithValue("@Login", Username);
                        insertCommand.Parameters.AddWithValue("@AvatarImage", imageData);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении аватара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow changePasswordWindow = new ChangePasswordWindow(Username);
            changePasswordWindow.ShowDialog();
        }

        private void Avatar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (UserAvatar != null)
            {
                // Создаем копию изображения
                var bitmap = UserAvatar.Clone();
                bitmap.Freeze();

                var viewer = new ImageViewerWindow(bitmap)
                {
                    Owner = Window.GetWindow(this) // Делаем родительским текущее окно
                };

                viewer.ShowDialog();
            }
        }
    }
}