using System.Data.SqlClient;

namespace RavenHub
{
    public class UserRepository
    {
        private readonly string _connectionString = @"Data Source=LAPTOP-REGTVFN9;Initial Catalog=RavenHub;Integrated Security=True;";

        public void RegisterUser(string login, string password, bool isAdmin)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO Users (Login, Password, IsAdmin) VALUES (@login, @password, @isAdmin)",
                    connection);
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", password); // Сохраняем пароль как текст
                command.Parameters.AddWithValue("@isAdmin", isAdmin);
                command.ExecuteNonQuery();
            }
        }

        public bool UserExists(string login)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE Login = @login",
                    connection);
                command.Parameters.AddWithValue("@login", login);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        public (bool isAuthenticated, bool isAdmin) VerifyUser(string login, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT Password, IsAdmin FROM Users WHERE Login = @login",
                    connection);
                command.Parameters.AddWithValue("@login", login);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return (false, false);

                    string storedPassword = reader.GetString(0); // Получаем пароль как текст
                    bool isAdmin = reader.GetBoolean(1);
                    bool isAuthenticated = storedPassword == password; // Сравниваем пароли напрямую
                    return (isAuthenticated, isAdmin);
                }
            }
        }
    }
}