using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RavenHub.Pages
{
    public partial class EmployeesPage : Page, INotifyPropertyChanged
    {
        private string connectionString = @"Data Source=LAPTOP-REGTVFN9; Initial Catalog=RavenHub; Integrated Security=True;";
        private DataTable _employeesTable;
        private DataRowView _selectedEmployee;
        private string _searchText;

        public DataTable EmployeesTable
        {
            get => _employeesTable;
            set { _employeesTable = value; OnPropertyChanged(nameof(EmployeesTable)); }
        }

        public DataRowView SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged(nameof(SelectedEmployee));
                OnPropertyChanged(nameof(HasSelectedEmployee));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterEmployees();
            }
        }

        public bool HasSelectedEmployee => SelectedEmployee != null;

        public ICommand AddEmployeeCommand { get; }
        public ICommand EditEmployeeCommand { get; }
        public ICommand DeleteEmployeeCommand { get; }
        public ICommand OpenSocialCommand { get; }
        public ICommand SearchCommand { get; }

        public EmployeesPage()
        {
            InitializeComponent();
            DataContext = this;

            AddEmployeeCommand = new RelayCommand(AddEmployee);
            EditEmployeeCommand = new RelayCommand(EditEmployee, _ => HasSelectedEmployee);
            DeleteEmployeeCommand = new RelayCommand(DeleteEmployee, _ => HasSelectedEmployee);
            OpenSocialCommand = new RelayCommand(OpenSocial);
            SearchCommand = new RelayCommand(_ => FilterEmployees());

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT e.*, p.PositionName AS Position
                        FROM Employees e
                        LEFT JOIN Positions p ON e.PositionId = p.PositionId";
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                    EmployeesTable = new DataTable();
                    dataAdapter.Fill(EmployeesTable);

                    EmployeesDataGrid.ItemsSource = EmployeesTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterEmployees()
        {
            if (EmployeesTable == null) return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                EmployeesTable.DefaultView.RowFilter = string.Empty;
            }
            else
            {
                EmployeesTable.DefaultView.RowFilter = $"FullName LIKE '%{SearchText}%'";
            }
        }

        private void AddEmployee(object parameter)
        {
            var positions = new ObservableCollection<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT PositionName FROM Positions", connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        positions.Add(reader.GetString(0));
                    }
                }
            }

            var window = new EmployeeCreateWindow(positions);
            if (window.ShowDialog() == true)
            {
                var newEmployeeData = window.EmployeeData;
                DataRow newRow = EmployeesTable.NewRow();
                newRow["FullName"] = newEmployeeData["FullName"];
                newRow["Position"] = newEmployeeData["Position"];
                newRow["PhoneNumber"] = newEmployeeData["PhoneNumber"];
                newRow["Email"] = newEmployeeData["Email"];
                newRow["SocialLink"] = newEmployeeData["SocialLink"];
                newRow["CreatedAt"] = DateTime.Now;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT PositionId FROM Positions WHERE PositionName = @PositionName", connection);
                    command.Parameters.AddWithValue("@PositionName", newEmployeeData["Position"]);
                    int positionId = (int)command.ExecuteScalar();
                    newRow["PositionId"] = positionId;

                    command = new SqlCommand("SELECT ISNULL(MAX(EmployeeId), 0) + 1 FROM Employees", connection);
                    newRow["EmployeeId"] = (int)command.ExecuteScalar();
                }

                EmployeesTable.Rows.Add(newRow);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Employees", connection);
                    SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                    dataAdapter.Update(EmployeesTable);
                }

                FilterEmployees();
            }
        }

        private void EditEmployee(object parameter)
        {
            if (SelectedEmployee == null) return;

            var positions = new ObservableCollection<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT PositionName FROM Positions", connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        positions.Add(reader.GetString(0));
                    }
                }
            }

            var employeeData = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Id", SelectedEmployee["EmployeeId"].ToString() },
                { "FullName", SelectedEmployee["FullName"].ToString() },
                { "Position", SelectedEmployee["Position"].ToString() },
                { "PhoneNumber", SelectedEmployee["PhoneNumber"].ToString() },
                { "Email", SelectedEmployee["Email"].ToString() },
                { "SocialLink", SelectedEmployee["SocialLink"].ToString() }
            };

            var window = new EmployeeEditWindow(employeeData, positions);

            if (window.ShowDialog() == true)
            {
                var updatedEmployeeData = window.EmployeeData;
                SelectedEmployee["FullName"] = updatedEmployeeData["FullName"];
                SelectedEmployee["Position"] = updatedEmployeeData["Position"];
                SelectedEmployee["PhoneNumber"] = updatedEmployeeData["PhoneNumber"];
                SelectedEmployee["Email"] = updatedEmployeeData["Email"];
                SelectedEmployee["SocialLink"] = updatedEmployeeData["SocialLink"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT PositionId FROM Positions WHERE PositionName = @PositionName", connection);
                    command.Parameters.AddWithValue("@PositionName", updatedEmployeeData["Position"]);
                    int positionId = (int)command.ExecuteScalar();
                    SelectedEmployee["PositionId"] = positionId;

                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Employees", connection);
                    SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                    dataAdapter.Update(EmployeesTable);
                }

                FilterEmployees();
            }
        }

        private void DeleteEmployee(object parameter)
        {
            if (MessageBox.Show("Удалить сотрудника?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SelectedEmployee.Row.Delete();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Employees", connection);
                    SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                    dataAdapter.Update(EmployeesTable);
                }
                FilterEmployees();
            }
        }

        private void OpenSocial(object parameter)
        {
            if (parameter is DataRowView row && !string.IsNullOrEmpty(row["SocialLink"]?.ToString()))
            {
                try
                {
                    var url = row["SocialLink"].ToString().StartsWith("http")
                        ? row["SocialLink"].ToString()
                        : $"https://{row["SocialLink"]}";

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии ссылки: {ex.Message}", "Ошибка", MessageBoxButton.OK);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}