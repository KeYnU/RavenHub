using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace RavenHub
{
    public class UserRepository : IDisposable
    {
        private readonly DatabaseConnectionService _dbService;

        public UserRepository(DatabaseConnectionService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }

        public async Task<(bool isAuthenticated, bool isAdmin)> VerifyUserAsync(string login, string password)
        {
            try
            {
                using (var connection = new SqlConnection(_dbService.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = new SqlCommand(
                        "SELECT Password, IsAdmin FROM Users WHERE Login = @login",
                        connection))
                    {
                        cmd.Parameters.Add("@login", SqlDbType.NVarChar, 50).Value = login.Trim();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync()) return (false, false);
                            return (password.Trim() == reader.GetString(0), reader.GetBoolean(1));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Ошибка аутентификации пользователя {login}");
                return (false, false);
            }
        }

        public async Task<bool> UserExistsAsync(string login)
        {
            try
            {
                using (var connection = new SqlConnection(_dbService.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Users WHERE Login = @login",
                        connection))
                    {
                        cmd.Parameters.Add("@login", SqlDbType.NVarChar, 50).Value = login.Trim();
                        int count = (int)await cmd.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Ошибка проверки существования пользователя {login}");
                throw;
            }
        }

        public async Task<bool> RegisterUserAsync(string login, string password, bool isAdmin = false)
        {
            try
            {
                using (var connection = new SqlConnection(_dbService.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = new SqlCommand(
                        "INSERT INTO Users (Login, Password, IsAdmin) VALUES (@login, @password, @isAdmin)",
                        connection))
                    {
                        cmd.Parameters.Add("@login", SqlDbType.NVarChar, 50).Value = login.Trim();
                        cmd.Parameters.Add("@password", SqlDbType.NVarChar, 100).Value = password.Trim();
                        cmd.Parameters.Add("@isAdmin", SqlDbType.Bit).Value = isAdmin;

                        int affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Ошибка регистрации пользователя {login}");
                throw;
            }
        }

        public void Dispose()
        {
            // Ресурсы освобождаются в DatabaseConnectionService
        }
    }
}