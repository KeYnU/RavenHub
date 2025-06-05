using System;
using System.Windows;
using System.Data.SqlClient;

namespace RavenHub
{
    public partial class ChangePasswordWindow : Window
    {
        private string connectionString = @"Data Source=.; Initial Catalog=RavenHub; Integrated Security=True;";
        private string currentUsername;

        public ChangePasswordWindow(string username)
        {
            InitializeComponent();
            currentUsername = username;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Проверка ввода
            if (string.IsNullOrEmpty(CurrentPasswordBox.Password) ||
                string.IsNullOrEmpty(NewPasswordBox.Password) ||
                string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка совпадения паролей
            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Новый пароль и подтверждение не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка сложности пароля
            if (NewPasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Новый пароль должен содержать не менее 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверка текущего пароля
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Login = @Login AND Password = @Password";
                    SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@Login", currentUsername);
                    checkCommand.Parameters.AddWithValue("@Password", CurrentPasswordBox.Password);

                    int count = (int)checkCommand.ExecuteScalar();

                    if (count == 0)
                    {
                        MessageBox.Show("Текущий пароль введен неверно", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Обновление пароля
                    string updateQuery = "UPDATE Users SET Password = @NewPassword WHERE Login = @Login";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@Login", currentUsername);
                    updateCommand.Parameters.AddWithValue("@NewPassword", NewPasswordBox.Password);
                    updateCommand.ExecuteNonQuery();

                    MessageBox.Show("Пароль успешно изменен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении пароля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}