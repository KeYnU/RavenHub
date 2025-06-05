using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RavenHub.Models;

namespace RavenHub.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly string _connectionString;

        public EmployeeService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<DataTable> GetEmployeesAsync()
        {
            return await Task.Run(() =>
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT e.*, p.PositionName AS Position
                        FROM Employees e
                        LEFT JOIN Positions p ON e.PositionId = p.PositionId";

                    var dataAdapter = new SqlDataAdapter(query, connection);
                    var dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    return dataTable;
                }
            });
        }

        public async Task<ObservableCollection<string>> GetPositionsAsync()
        {
            return await Task.Run(() =>
            {
                var positions = new ObservableCollection<string>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT PositionName FROM Positions", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            positions.Add(reader.GetString(0));
                        }
                    }
                }
                return positions;
            });
        }

        public async Task AddEmployeeAsync(Employee employee)
        {
            await Task.Run(() =>
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Получаем PositionId по названию должности
                    var getPositionIdCommand = new SqlCommand(
                        "SELECT PositionId FROM Positions WHERE PositionName = @PositionName",
                        connection);
                    getPositionIdCommand.Parameters.AddWithValue("@PositionName", employee.Position);
                    var positionId = (int)getPositionIdCommand.ExecuteScalar();

                    var command = new SqlCommand(@"
                        INSERT INTO Employees (FullName, PositionId, PhoneNumber, Email, SocialLink, CreatedAt)
                        VALUES (@FullName, @PositionId, @PhoneNumber, @Email, @SocialLink, @CreatedAt)", connection);

                    command.Parameters.AddWithValue("@FullName", employee.FullName);
                    command.Parameters.AddWithValue("@PositionId", positionId);
                    command.Parameters.AddWithValue("@PhoneNumber", employee.PhoneNumber);
                    command.Parameters.AddWithValue("@Email", employee.Email);
                    command.Parameters.AddWithValue("@SocialLink", employee.SocialLink ?? "");
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    command.ExecuteNonQuery();
                }
            });
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            await Task.Run(() =>
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Получаем PositionId по названию должности
                    var getPositionIdCommand = new SqlCommand(
                        "SELECT PositionId FROM Positions WHERE PositionName = @PositionName",
                        connection);
                    getPositionIdCommand.Parameters.AddWithValue("@PositionName", employee.Position);
                    var positionId = (int)getPositionIdCommand.ExecuteScalar();

                    var command = new SqlCommand(@"
                        UPDATE Employees 
                        SET FullName = @FullName, 
                            PositionId = @PositionId, 
                            PhoneNumber = @PhoneNumber, 
                            Email = @Email, 
                            SocialLink = @SocialLink
                        WHERE EmployeeId = @EmployeeId", connection);

                    command.Parameters.AddWithValue("@EmployeeId", employee.EmployeeId);
                    command.Parameters.AddWithValue("@FullName", employee.FullName);
                    command.Parameters.AddWithValue("@PositionId", positionId);
                    command.Parameters.AddWithValue("@PhoneNumber", employee.PhoneNumber);
                    command.Parameters.AddWithValue("@Email", employee.Email);
                    command.Parameters.AddWithValue("@SocialLink", employee.SocialLink ?? "");

                    command.ExecuteNonQuery();
                }
            });
        }

        public async Task DeleteEmployeeAsync(int employeeId)
        {
            await Task.Run(() =>
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("DELETE FROM Employees WHERE EmployeeId = @EmployeeId", connection);
                    command.Parameters.AddWithValue("@EmployeeId", employeeId);
                    command.ExecuteNonQuery();
                }
            });
        }

        public void OpenSocialLink(string link)
        {
            try
            {
                var url = link.StartsWith("http") ? link : $"https://{link}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при открытии ссылки: {ex.Message}");
            }
        }


        public void OpenFile(string filePath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
    }
}