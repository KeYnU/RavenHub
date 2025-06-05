using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace RavenHub.Pages
{
    public partial class StatisticsPage : Page, INotifyPropertyChanged
    {
        private string connectionString = @"Data Source=.; Initial Catalog=RavenHub; Integrated Security=True;";
        private int _soldBox;
        private int _gotBox;
        private int _overallBox;
        private double _soldProgress;
        private double _gotProgress;

        public int SoldBox
        {
            get => _soldBox;
            set
            {
                _soldBox = value;
                OnPropertyChanged(nameof(SoldBox));
                UpdateProgress();
            }
        }

        public int GotBox
        {
            get => _gotBox;
            set
            {
                _gotBox = value;
                OnPropertyChanged(nameof(GotBox));
                UpdateProgress();
            }
        }

        public int OverallBox
        {
            get => _overallBox;
            set
            {
                _overallBox = value;
                OnPropertyChanged(nameof(OverallBox));
            }
        }

        public double SoldProgress
        {
            get => _soldProgress;
            set
            {
                _soldProgress = value;
                OnPropertyChanged(nameof(SoldProgress));
            }
        }

        public double GotProgress
        {
            get => _gotProgress;
            set
            {
                _gotProgress = value;
                OnPropertyChanged(nameof(GotProgress));
            }
        }

        public StatisticsPage()
        {
            InitializeComponent();
            LoadData();
            this.DataContext = this;
        }

        private void LoadData()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT SUM(SoldBox) AS SoldBox, SUM(GotBox) AS GotBox FROM Statisticc", connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        SoldBox = dataTable.Rows[0].Field<int>("SoldBox");
                        GotBox = dataTable.Rows[0].Field<int>("GotBox");
                        OverallBox = SoldBox + GotBox;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProgress()
        {
            if (OverallBox > 0)
            {
                SoldProgress = (SoldBox * 100.0) / OverallBox;
                GotProgress = (GotBox * 100.0) / OverallBox;
            }
            else
            {
                SoldProgress = 0;
                GotProgress = 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}