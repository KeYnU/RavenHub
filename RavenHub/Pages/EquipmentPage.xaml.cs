using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace RavenHub.Pages
{
    public partial class EquipmentPage : Page, INotifyPropertyChanged
    {
        private string connectionString = @"Data Source=.;Initial Catalog=RavenHub;Integrated Security=True;";
        private int _serverCount;
        private int _pcCount;
        private int _laptopCount;
        private DataTable _equipmentTable;

        public int ServerCount
        {
            get => _serverCount;
            set { _serverCount = value; OnPropertyChanged(nameof(ServerCount)); UpdateTotal(); }
        }

        public int PcCount
        {
            get => _pcCount;
            set { _pcCount = value; OnPropertyChanged(nameof(PcCount)); UpdateTotal(); }
        }

        public int LaptopCount
        {
            get => _laptopCount;
            set { _laptopCount = value; OnPropertyChanged(nameof(LaptopCount)); UpdateTotal(); }
        }

        public int TotalCount { get; private set; }

        public DataTable EquipmentTable
        {
            get => _equipmentTable;
            set { _equipmentTable = value; OnPropertyChanged(nameof(EquipmentTable)); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand AddEquipmentCommand { get; }

        public EquipmentPage()
        {
            InitializeComponent();
            DataContext = this;

            // Инициализация команд
            RefreshCommand = new RelayCommand(_ => RefreshData());
            SaveCommand = new RelayCommand(_ => SaveData(), _ => CanSave());
            ExportCommand = new RelayCommand(_ => ExportData(), _ => CanExport());
            AddEquipmentCommand = new RelayCommand(_ => AddEquipment());

            // Загрузка данных
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Equipment", connection);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    EquipmentTable = dt;
                    EquipmentListDataGrid.ItemsSource = EquipmentTable.DefaultView;

                    CalculateCounts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateCounts()
        {
            if (EquipmentTable == null) return;

            ServerCount = EquipmentTable.AsEnumerable()
                .Where(row => row.Field<string>("Type") == "Сервер")
                .Sum(row => row.Field<int>("Quantity"));
            PcCount = EquipmentTable.AsEnumerable()
                .Where(row => row.Field<string>("Type") == "Компьютер")
                .Sum(row => row.Field<int>("Quantity"));
            LaptopCount = EquipmentTable.AsEnumerable()
                .Where(row => row.Field<string>("Type") == "Ноутбук")
                .Sum(row => row.Field<int>("Quantity"));
        }

        private void UpdateTotal()
        {
            TotalCount = ServerCount + PcCount + LaptopCount;
            OnPropertyChanged(nameof(TotalCount));
        }

        private void RefreshData()
        {
            try
            {
                LoadData();
                ShowMessage("Данные успешно обновлены");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveData()
        {
            try
            {
                if (EquipmentTable == null || EquipmentTable.GetChanges() == null)
                {
                    ShowMessage("Нет изменений для сохранения");
                    return;
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Equipment", connection);
                    SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                    int rowsAffected = dataAdapter.Update(EquipmentTable);

                    EquipmentTable.AcceptChanges();
                    ShowMessage($"Успешно сохранено изменений: {rowsAffected}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSave()
        {
            return EquipmentTable != null && EquipmentTable.GetChanges() != null;
        }

        private void ExportData()
        {
            try
            {
                if (EquipmentTable == null || EquipmentTable.Rows.Count == 0)
                {
                    ShowMessage("Нет данных для экспорта");
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Оборудование_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string extension = Path.GetExtension(filePath);

                    if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportToCsv(filePath);
                    }
                    else if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportToExcel(filePath);
                    }

                    ShowMessage($"Данные успешно экспортированы в файл: {filePath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            // Заголовки
            var headers = EquipmentTable.Columns.Cast<DataColumn>()
                .Select(column => column.ColumnName);
            sb.AppendLine(string.Join(";", headers));

            // Данные
            foreach (DataRow row in EquipmentTable.Rows)
            {
                var fields = row.ItemArray.Select(field =>
                    field.ToString().Replace(";", ","));
                sb.AppendLine(string.Join(";", fields));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private void ExportToExcel(string filePath)
        {
            // Для экспорта в Excel нужно добавить ссылку на Microsoft.Office.Interop.Excel
            // или использовать библиотеку типа EPPlus
            MessageBox.Show("Экспорт в Excel требует дополнительных библиотек. Экспортируем в CSV вместо этого.",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            ExportToCsv(Path.ChangeExtension(filePath, ".csv"));
        }

        private bool CanExport()
        {
            return EquipmentTable != null && EquipmentTable.Rows.Count > 0;
        }

        private void AddEquipment()
        {
            try
            {
                if (EquipmentTable == null) return;

                DataRow newRow = EquipmentTable.NewRow();
                newRow["Type"] = "Новое";
                newRow["Model"] = "Модель";
                newRow["Quantity"] = 1;
                newRow["CreatedAt"] = DateTime.Now;
                EquipmentTable.Rows.Add(newRow);

                // Прокрутка и фокус на новой строке
                EquipmentListDataGrid.ScrollIntoView(newRow);
                EquipmentListDataGrid.SelectedItem = newRow;
                EquipmentListDataGrid.Focus();

                ShowMessage("Добавлено новое оборудование");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message, "RavenHub", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}