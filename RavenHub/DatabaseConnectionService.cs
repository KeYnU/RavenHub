using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace RavenHub
{
    public class DatabaseConnectionService : IDisposable
    {
        private readonly string _connectionString;
        private SqlConnection _connection;

        public string ConnectionString => _connectionString;

        public DatabaseConnectionService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<(bool IsConnected, string ErrorMessage)> TestConnectionAsync()
        {
            try
            {
                _connection = new SqlConnection(_connectionString);
                await _connection.OpenAsync();
                _connection.Close(); // Заменяем CloseAsync на Close
                return (true, null);
            }
            catch (SqlException sqlEx)
            {
                return (false, $"SQL Error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"General Error: {ex.Message}");
            }
        }

        public async Task<bool> WaitForConnectionAsync(int timeoutSeconds = 15)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);

            while (DateTime.Now < endTime)
            {
                var (connected, _) = await TestConnectionAsync();
                if (connected) return true;

                await Task.Delay(2000);
            }

            return false;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}