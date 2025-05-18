using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WpfFrame = System.Windows.Controls.Frame;

namespace RavenHub.Pages
{
    public partial class EmployeesPage : Page, INotifyPropertyChanged, IDisposable
    {
        public WpfFrame NavigationFrame { get; }
        public string Username { get; }
        public bool IsAdmin { get; }

        private string connectionString = @"Data Source=.; Initial Catalog=RavenHub; Integrated Security=True;";
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
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    FilterEmployees();
                }
            }
        }

        public bool HasSelectedEmployee => SelectedEmployee != null;

        public ICommand AddEmployeeCommand { get; }
        public ICommand EditEmployeeCommand { get; }
        public ICommand DeleteEmployeeCommand { get; }
        public ICommand OpenSocialCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ExportToCsvCommand { get; }
        public ICommand ExportToDocxCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public EmployeesPage()
        {
            InitializeComponent();
            DataContext = this;

            AddEmployeeCommand = new RelayCommand(_ => AddEmployee());
            EditEmployeeCommand = new RelayCommand(_ => EditEmployee(), _ => HasSelectedEmployee);
            DeleteEmployeeCommand = new RelayCommand(_ => DeleteEmployee(), _ => HasSelectedEmployee);
            OpenSocialCommand = new RelayCommand(parameter => OpenSocial(parameter));
            SearchCommand = new RelayCommand(_ => FilterEmployees());
            ExportToCsvCommand = new RelayCommand(_ => ExportToCsv());
            ExportToDocxCommand = new RelayCommand(_ => ExportToDocx(), _ => IsAdmin);

            LoadData();
        }

        public EmployeesPage(WpfFrame navigationFrame, string username, bool isAdmin)
            : this()
        {
            NavigationFrame = navigationFrame;
            Username = username;
            IsAdmin = isAdmin;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private void AddEmployee()
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

        private void EditEmployee()
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

            var employeeData = new Dictionary<string, string>
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

        private void DeleteEmployee()
        {
            if (SelectedEmployee == null) return;

            if (MessageBox.Show("Удалить сотрудника?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                    MessageBox.Show($"Ошибка при открытии ссылки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToCsv()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = "Сотрудники.csv",
                DefaultExt = ".csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var currentView = EmployeesTable.DefaultView;

                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        var headers = EmployeesTable.Columns
                            .Cast<DataColumn>()
                            .Where(c => c.ColumnName != "PositionId" && c.ColumnName != "CreatedAt")
                            .Select(c => c.ColumnName);
                        writer.WriteLine(string.Join(";", headers));

                        foreach (DataRowView rowView in currentView)
                        {
                            var row = rowView.Row;
                            var values = new List<string>();
                            foreach (DataColumn col in EmployeesTable.Columns)
                            {
                                if (col.ColumnName != "PositionId" && col.ColumnName != "CreatedAt")
                                {
                                    var value = row[col].ToString();
                                    if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
                                    {
                                        value = $"\"{value.Replace("\"", "\"\"")}\"";
                                    }
                                    values.Add(value);
                                }
                            }
                            writer.WriteLine(string.Join(";", values));
                        }
                    }

                    MessageBox.Show("Экспорт завершен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (MessageBox.Show("Открыть экспортированный файл?", "Открыть файл",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToDocx()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Word Documents (*.docx)|*.docx",
                FileName = "Сотрудники_" + DateTime.Now.ToString("yyyyMMdd") + ".docx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var document = WordprocessingDocument.Create(saveFileDialog.FileName, WordprocessingDocumentType.Document))
                    {
                        // Основная часть документа
                        var mainPart = document.AddMainDocumentPart();
                        mainPart.Document = new Document();
                        var body = mainPart.Document.AppendChild(new Body());

                        // Настройки страницы
                        var sectionProps = new SectionProperties(
                            new PageMargin()
                            {
                                Top = 1000,
                                Bottom = 1000,
                                Left = 1000,
                                Right = 1000
                            });
                        body.AppendChild(sectionProps);

                        // Заголовок документа
                        var title = new Paragraph(
                            new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Center },
                                new SpacingBetweenLines() { After = "200" }
                            ),
                            new Run(
                                new RunProperties(
                                    new FontSize() { Val = "28" },
                                    new Bold()
                                ),
                                new Text("Список сотрудников")
                            )
                        );
                        body.AppendChild(title);

                        // Дата экспорта
                        var date = new Paragraph(
                            new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Right },
                                new SpacingBetweenLines() { After = "200" }
                            ),
                            new Run(
                                new RunProperties(
                                    new FontSize() { Val = "14" }
                                ),
                                new Text($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm}")
                            )
                        );
                        body.AppendChild(date);

                        // Создаем таблицу с автоматическим подбором ширины
                        var table = new Table();

                        // Настройки таблицы (автоматическая ширина)
                        var tableProperties = new TableProperties(
                            new TableBorders(
                                new TopBorder() { Val = BorderValues.Single, Size = 4 },
                                new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                                new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                                new RightBorder() { Val = BorderValues.Single, Size = 4 },
                                new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 2 },
                                new InsideVerticalBorder() { Val = BorderValues.Single, Size = 2 }
                            ),
                            new TableWidth() { Width = "100%", Type = TableWidthUnitValues.Pct },
                            new TableLayout() { Type = TableLayoutValues.Autofit }
                        );
                        table.AppendChild(tableProperties);

                        // Заголовки таблицы
                        var headerRow = new TableRow();

                        // Ширина колонок в процентах
                        string[] headers = { "ФИО (30%)", "Должность (20%)", "Телефон (15%)", "Email (20%)", "Соцсети (15%)" };
                        int[] widths = { 30, 20, 15, 20, 15 };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = new TableCell(
                                new TableCellProperties(
                                    new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = (widths[i] * 50).ToString() }
                                ),
                                new Paragraph(
                                    new ParagraphProperties(
                                        new Justification() { Val = JustificationValues.Center }
                                    ),
                                    new Run(
                                        new RunProperties(new Bold()),
                                        new Text(headers[i].Split(' ')[0])
                                    )
                                )
                            );
                            headerRow.AppendChild(cell);
                        }
                        table.AppendChild(headerRow);

                        // Данные сотрудников
                        foreach (DataRowView rowView in EmployeesTable.DefaultView)
                        {
                            var row = rowView.Row;
                            var dataRow = new TableRow();

                            // Создаем ячейки с данными
                            var cellsData = new[]
                            {
                        new { Value = row["FullName"]?.ToString() ?? "", Width = widths[0] },
                        new { Value = row["Position"]?.ToString() ?? "", Width = widths[1] },
                        new { Value = FormatPhoneNumber(row["PhoneNumber"]?.ToString()), Width = widths[2] },
                        new { Value = row["Email"]?.ToString() ?? "", Width = widths[3] },
                        new { Value = FormatSocialLink(row["SocialLink"]?.ToString()), Width = widths[4] }
                    };

                            foreach (var cellData in cellsData)
                            {
                                var cell = new TableCell(
                                    new TableCellProperties(
                                        new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = (cellData.Width * 50).ToString() },
                                        new VerticalMerge() { Val = MergedCellValues.Restart }
                                    ),
                                    new Paragraph(
                                        new Run(
                                            new Text(cellData.Value)
                                        )
                                    )
                                );
                                dataRow.AppendChild(cell);
                            }

                            table.AppendChild(dataRow);
                        }

                        body.AppendChild(table);
                    }

                    MessageBox.Show("Документ успешно экспортирован!", "Готово",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Форматирование телефонного номера
        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return "";

            // Удаляем все нецифровые символы
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11) // Российский номер
            {
                return $"+7 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9)}";
            }

            return phone; // Возвращаем как есть, если формат не распознан
        }

        // Форматирование ссылки на соцсети
        private string FormatSocialLink(string link)
        {
            if (string.IsNullOrEmpty(link)) return "";

            // Убираем "http://" или "https://" для краткости
            return link.Replace("https://", "").Replace("http://", "");
        }

        public void Dispose()
        {
            EmployeesTable?.Dispose();
        }
    }
}