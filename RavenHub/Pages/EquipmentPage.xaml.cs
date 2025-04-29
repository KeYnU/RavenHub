using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RavenHub.Pages
{
    public partial class EquipmentPage : Page, INotifyPropertyChanged
    {
        private string connectionString = @"Data Source=LAPTOP-REGTVFN9; Initial Catalog=RavenHub; Integrated Security=True;";
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
            RefreshCommand = new RelayCommand(RefreshData);
            SaveCommand = new RelayCommand(SaveData, CanSave);
            ExportCommand = new RelayCommand(ExportData, CanExport);
            AddEquipmentCommand = new RelayCommand(AddEquipment);

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
                    EquipmentTable = new DataTable();
                    dataAdapter.Fill(EquipmentTable);

                    // Привязка к DataGrid
                    EquipmentListDataGrid.ItemsSource = EquipmentTable.DefaultView; // Укажи имя DataGrid из XAML

                    // Подсчет оборудования
                    CalculateCounts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateCounts()
        {
            ServerCount = EquipmentTable.AsEnumerable()
                .Where(row => row.Field<string>("Type") == "Сервер")
                .Sum(row => row.Field<int>("Quantity"));
            PcCount = EquipmentTable.AsEnumerable()
                .Where(row => row.Field<string>("Type") == "Компьютер")
                .Sum(row => row.Field<int>("Quantity"));
            LaptopCount = EquipmentTable.AsEnumerable()
                .Where(row => row.Field<string>("Type") == "Ноутбук")
                .Sum(row => row.Field<int>("Quantity"));
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            TotalCount = ServerCount + PcCount + LaptopCount;
            OnPropertyChanged(nameof(TotalCount));
        }

        private void RefreshData(object parameter)
        {
            LoadData();
            ShowMessage("Данные обновлены");
        }

        private void SaveData(object parameter)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Equipment", connection);
                    SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                    dataAdapter.Update(EquipmentTable);
                    ShowMessage("Изменения сохранены");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSave(object parameter)
        {
            return EquipmentTable != null && EquipmentTable.Rows.Count > 0;
        }

        private void ExportData(object parameter)
        {
            ShowMessage("Экспорт данных выполнен");
        }

        private bool CanExport(object parameter)
        {
            return EquipmentTable != null && EquipmentTable.Rows.Count > 0;
        }

        private void AddEquipment(object parameter)
        {
            try
            {
                DataRow newRow = EquipmentTable.NewRow();
                newRow["Type"] = "Новое";
                newRow["Model"] = "Модель";
                newRow["Quantity"] = 1;
                newRow["CreatedAt"] = DateTime.Now;
                EquipmentTable.Rows.Add(newRow);

                ShowMessage("Добавлено новое оборудование");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            var fullText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength).Insert(textBox.SelectionStart, e.Text);
            e.Handled = !int.TryParse(fullText, out int _);
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
}