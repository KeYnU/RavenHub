using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RavenHub.Pages
{
    public partial class TasksPage : Page, INotifyPropertyChanged
    {
        private string connectionString = @"Data Source=LAPTOP-REGTVFN9; Initial Catalog=RavenHub; Integrated Security=True;";
        private DataTable _tasksTable;
        private DataRowView _selectedTask;
        private string _searchText;
        private string _selectedStatusFilter;

        public DataTable TasksTable
        {
            get => _tasksTable;
            set { _tasksTable = value; OnPropertyChanged(nameof(TasksTable)); }
        }

        public DataRowView SelectedTask
        {
            get => _selectedTask;
            set { _selectedTask = value; OnPropertyChanged(nameof(SelectedTask)); }
        }

        public bool HasSelectedTask => SelectedTask != null;

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); FilterTasks(); }
        }

        public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
        {
            "Все", "Активные", "Просроченные", "Завершенные"
        };

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { _selectedStatusFilter = value; OnPropertyChanged(nameof(SelectedStatusFilter)); FilterTasks(); }
        }

        public ICommand CreateTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ToggleViewCommand { get; }

        public TasksPage()
        {
            InitializeComponent();
            DataContext = this;

            CreateTaskCommand = new RelayCommand(_ => CreateTask());
            EditTaskCommand = new RelayCommand(_ => EditTask(), _ => HasSelectedTask);
            DeleteTaskCommand = new RelayCommand(_ => DeleteTask(), _ => HasSelectedTask);
            ToggleViewCommand = new RelayCommand(_ => ToggleView());

            SelectedStatusFilter = StatusFilters.First();
            InitializeTasks();
        }

        private void InitializeTasks()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Tasks", connection);
                    TasksTable = new DataTable();
                    dataAdapter.Fill(TasksTable);

                    TasksDataGrid.ItemsSource = TasksTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTasks()
        {
            if (TasksTable == null) return;

            string filter = "";
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filter = $"Title LIKE '%{SearchText}%' OR Description LIKE '%{SearchText}%'";
            }

            switch (SelectedStatusFilter)
            {
                case "Активные":
                    filter = string.IsNullOrEmpty(filter)
                        ? "IsCompleted = 0 AND Deadline >= GETDATE()"
                        : $"{filter} AND IsCompleted = 0 AND Deadline >= GETDATE()";
                    break;
                case "Просроченные":
                    filter = string.IsNullOrEmpty(filter)
                        ? "IsCompleted = 0 AND Deadline < GETDATE()"
                        : $"{filter} AND IsCompleted = 0 AND Deadline < GETDATE()";
                    break;
                case "Завершенные":
                    filter = string.IsNullOrEmpty(filter)
                        ? "IsCompleted = 1"
                        : $"{filter} AND IsCompleted = 1";
                    break;
            }

            TasksTable.DefaultView.RowFilter = filter;
        }

        private void CreateTask()
        {
            var window = new CreateTaskWindow();
            if (window.ShowDialog() == true)
            {
                var taskData = window.TaskData;
                DataRow newRow = TasksTable.NewRow();
                newRow["Title"] = taskData["Title"];
                newRow["Description"] = taskData["Description"];
                newRow["Deadline"] = DateTime.Now.AddDays(1);
                newRow["IsCompleted"] = false;
                newRow["CreatedAt"] = DateTime.Now;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT ISNULL(MAX(TaskId), 0) + 1 FROM Tasks", connection);
                    newRow["TaskId"] = (int)command.ExecuteScalar();

                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Tasks", connection);
                    SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                    TasksTable.Rows.Add(newRow);
                    dataAdapter.Update(TasksTable);
                }

                FilterTasks();
            }
        }

        private void EditTask()
        {
            var window = new CreateTaskWindow();
            window.Title = "Редактирование задачи";
            window.TitleTextBox.Text = SelectedTask["Title"].ToString();
            window.DescriptionTextBox.Text = SelectedTask["Description"].ToString();

            if (window.ShowDialog() == true)
            {
                var taskData = window.TaskData;
                SelectedTask["Title"] = taskData["Title"];
                SelectedTask["Description"] = taskData["Description"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Tasks", connection);
                    SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                    dataAdapter.Update(TasksTable);
                }

                FilterTasks();
            }
        }

        private void DeleteTask()
        {
            if (MessageBox.Show("Удалить выбранную задачу?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SelectedTask.Row.Delete();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Tasks", connection);
                    SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                    dataAdapter.Update(TasksTable);
                }
                FilterTasks();
            }
        }

        private void ToggleView()
        {
            // Логика переключения вида (если нужна)
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCompleted && targetType == typeof(MaterialDesignThemes.Wpf.PackIconKind))
            {
                var row = (parameter as System.Data.DataRowView)?.Row;
                if (row == null) return MaterialDesignThemes.Wpf.PackIconKind.ProgressClock;

                DateTime deadline = row.Field<DateTime>("Deadline");
                if (isCompleted)
                    return MaterialDesignThemes.Wpf.PackIconKind.CheckBold;
                else if (deadline < DateTime.Now)
                    return MaterialDesignThemes.Wpf.PackIconKind.Alert;
                else
                    return MaterialDesignThemes.Wpf.PackIconKind.ProgressClock;
            }
            return MaterialDesignThemes.Wpf.PackIconKind.ProgressClock;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCompleted && targetType == typeof(Brush))
            {
                var row = (parameter as System.Data.DataRowView)?.Row;
                if (row == null) return Brushes.Orange;

                DateTime deadline = row.Field<DateTime>("Deadline");
                if (isCompleted)
                    return Brushes.Green;
                else if (deadline < DateTime.Now)
                    return Brushes.Red;
                else
                    return Brushes.Orange;
            }
            return Brushes.Orange;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}