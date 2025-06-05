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
using RavenHub.Pages;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace RavenHub.Pages
{
    public partial class TasksPage : Page, INotifyPropertyChanged
    {
        private string connectionString = @"Data Source=.; Initial Catalog=RavenHub; Integrated Security=True;";
        private DataTable _tasksTable;
        private DataRowView _selectedTask;
        private string _searchText;
        private string _selectedStatusFilter;

        public DataTable TasksTable
        {
            get { return _tasksTable; }
            set { _tasksTable = value; OnPropertyChanged(nameof(TasksTable)); }
        }

        public DataRowView SelectedTask
        {
            get { return _selectedTask; }
            set { _selectedTask = value; OnPropertyChanged(nameof(SelectedTask)); OnPropertyChanged(nameof(HasSelectedTask)); }
        }

        public bool HasSelectedTask => SelectedTask != null;

        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); FilterTasks(); }
        }

        public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
        {
            "Все", "Активные", "Просроченные", "Завершенные"
        };

        public string SelectedStatusFilter
        {
            get { return _selectedStatusFilter; }
            set { _selectedStatusFilter = value; OnPropertyChanged(nameof(SelectedStatusFilter)); FilterTasks(); }
        }

        public ICommand CreateTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }

        public TasksPage()
        {
            InitializeComponent();
            DataContext = this;

            CreateTaskCommand = new RelayCommand(_ => CreateTask());
            EditTaskCommand = new RelayCommand(_ => EditTask(), _ => HasSelectedTask);
            DeleteTaskCommand = new RelayCommand(_ => DeleteTask(), _ => HasSelectedTask);

            SelectedStatusFilter = StatusFilters.First();
            InitializeTasks();

            TasksDataGrid.LoadingRow += TasksDataGrid_LoadingRow;
        }

        private void TasksDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is DataRowView rowView)
            {
                e.Row.Tag = rowView;
            }
        }

        private bool TestConnection()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void InitializeTasks()
        {
            if (!TestConnection())
            {
                MessageBox.Show("Не удалось подключиться к базе данных", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Tasks", connection);
                    DataTable newTable = new DataTable();
                    dataAdapter.Fill(newTable);

                    TasksTable = newTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке задач: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTasks()
        {
            if (TasksTable == null) return;

            TaskFilter.ApplyFilter(
                TasksTable.DefaultView,
                SearchText,
                SelectedStatusFilter
            );
        }

        private void CreateTask()
        {
            var window = new CreateTaskWindow();
            if (window.ShowDialog() == true)
            {
                try
                {
                    var taskData = window.TaskData;
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        SqlCommand getIdCommand = new SqlCommand("SELECT ISNULL(MAX(TaskId), 0) + 1 FROM Tasks", connection);
                        int newId = (int)getIdCommand.ExecuteScalar();

                        SqlCommand insertCommand = new SqlCommand(
                            "INSERT INTO Tasks (TaskId, Title, Description, Deadline, IsCompleted, CreatedAt) " +
                            "VALUES (@TaskId, @Title, @Description, @Deadline, @IsCompleted, @CreatedAt)", connection);

                        insertCommand.Parameters.AddWithValue("@TaskId", newId);
                        insertCommand.Parameters.AddWithValue("@Title", taskData["Title"]);
                        insertCommand.Parameters.AddWithValue("@Description", taskData["Description"]);

                        object deadlineValue = taskData.ContainsKey("Deadline") ?
                            taskData["Deadline"] :
                            (object)DateTime.Now.AddDays(1);
                        insertCommand.Parameters.AddWithValue("@Deadline", deadlineValue);

                        insertCommand.Parameters.AddWithValue("@IsCompleted", false);
                        insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                        insertCommand.ExecuteNonQuery();
                    }

                    InitializeTasks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                try
                {
                    var taskData = window.TaskData;
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        SqlCommand updateCommand = new SqlCommand(
                            "UPDATE Tasks SET Title = @Title, Description = @Description " +
                            "WHERE TaskId = @TaskId", connection);

                        updateCommand.Parameters.AddWithValue("@TaskId", SelectedTask["TaskId"]);
                        updateCommand.Parameters.AddWithValue("@Title", taskData["Title"]);
                        updateCommand.Parameters.AddWithValue("@Description", taskData["Description"]);

                        updateCommand.ExecuteNonQuery();
                    }

                    InitializeTasks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteTask()
        {
            if (MessageBox.Show("Удалить выбранную задачу?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        SqlCommand deleteCommand = new SqlCommand(
                            "DELETE FROM Tasks WHERE TaskId = @TaskId", connection);

                        deleteCommand.Parameters.AddWithValue("@TaskId", SelectedTask["TaskId"]);
                        deleteCommand.ExecuteNonQuery();
                    }

                    InitializeTasks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
            if (!(value is bool isCompleted))
                return PackIconKind.ProgressClock;

            if (!(parameter is DataRowView rowView))
                return PackIconKind.ProgressClock;

            try
            {
                DateTime deadline = rowView["Deadline"] is DateTime dt ? dt : DateTime.MinValue;

                if (isCompleted)
                    return PackIconKind.CheckBold;
                if (deadline < DateTime.Now)
                    return PackIconKind.Alert;

                return PackIconKind.ProgressClock;
            }
            catch
            {
                return PackIconKind.ProgressClock;
            }
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
            if (!(value is bool isCompleted))
                return Brushes.Orange;

            if (!(parameter is DataRowView rowView))
                return Brushes.Orange;

            try
            {
                DateTime deadline = rowView["Deadline"] is DateTime dt ? dt : DateTime.MinValue;

                if (isCompleted)
                    return Brushes.Green;
                if (deadline < DateTime.Now)
                    return Brushes.Red;

                return Brushes.Orange;
            }
            catch
            {
                return Brushes.Orange;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}