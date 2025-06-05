using System.Windows.Controls;
using RavenHub.ViewModels;
using RavenHub.Services;

namespace RavenHub.Pages
{
    public partial class EmployeesPage : Page
    {
        private readonly EmployeesViewModel _viewModel;

        public EmployeesPage(Frame navigationFrame, string username, bool isAdmin)
        {
            InitializeComponent();

            // Инициализация сервисов
            var connectionString = @"Data Source=.; Initial Catalog=RavenHub; Integrated Security=True;";
            var employeeService = new EmployeeService(connectionString);
            var exportService = new ExportService();
            var documentService = new DocumentService();
            var dialogService = new DialogService();

            // Создание ViewModel
            _viewModel = new EmployeesViewModel(employeeService, exportService, documentService, dialogService)
            {
                IsAdmin = isAdmin
            };

            DataContext = _viewModel;
        }

        // Удалите эти методы - они больше не нужны
        // private void GenerateEmploymentDocument_Click(object sender, System.Windows.RoutedEventArgs e)
        // private void GenerateDismissalDocument_Click(object sender, System.Windows.RoutedEventArgs e)
    }
}