using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RavenHub;
using RavenHub.Services;
using RavenHub.Models;
using RavenHub.Helpers;
using System.Threading.Tasks;

namespace RavenHub.ViewModels
{
    public class EmployeesViewModel : INotifyPropertyChanged
    {
        private readonly IEmployeeService _employeeService;
        private readonly IExportService _exportService;
        private readonly IDocumentService _documentService;
        private readonly IDialogService _dialogService;

        private DataTable _employeesTable;
        private DataRowView _selectedEmployee;
        private string _searchText;
        private bool _isAdmin;

        public DataTable EmployeesTable
        {
            get => _employeesTable;
            set { _employeesTable = value; OnPropertyChanged(); }
        }

        public DataRowView SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
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
                    OnPropertyChanged();
                    FilterEmployees();
                }
            }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(); }
        }

        public bool HasSelectedEmployee => SelectedEmployee != null;

        // Commands
        public ICommand AddEmployeeCommand { get; }
        public ICommand EditEmployeeCommand { get; }
        public ICommand DeleteEmployeeCommand { get; }
        public ICommand OpenSocialCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ExportToCsvCommand { get; }
        public ICommand ExportToDocxCommand { get; }
        public ICommand GenerateEmploymentDocumentCommand { get; }
        public ICommand GenerateDismissalDocumentCommand { get; }
        public ICommand GenerateEmploymentDocumentWordCommand { get; }
        public ICommand GenerateEmploymentDocumentPdfCommand { get; }
        public ICommand GenerateDismissalDocumentWordCommand { get; }
        public ICommand GenerateDismissalDocumentPdfCommand { get; }

        public EmployeesViewModel(
            IEmployeeService employeeService,
            IExportService exportService,
            IDocumentService documentService,
            IDialogService dialogService)
        {
            _employeeService = employeeService;
            _exportService = exportService;
            _documentService = documentService;
            _dialogService = dialogService;

            // Initialize commands
            AddEmployeeCommand = new RelayCommand(ExecuteAddEmployee, CanExecuteAdminCommand);
            EditEmployeeCommand = new RelayCommand(ExecuteEditEmployee, CanExecuteEditDelete);
            DeleteEmployeeCommand = new RelayCommand(ExecuteDeleteEmployee, CanExecuteEditDelete);
            OpenSocialCommand = new RelayCommand(ExecuteOpenSocial);
            SearchCommand = new RelayCommand(ExecuteSearch);
            ExportToCsvCommand = new RelayCommand(ExecuteExportToCsv, CanExecuteAdminCommand);
            ExportToDocxCommand = new RelayCommand(ExecuteExportToDocx, CanExecuteAdminCommand);
            GenerateEmploymentDocumentCommand = new RelayCommand(ExecuteGenerateEmploymentDocument, CanExecuteEditDelete);
            GenerateDismissalDocumentCommand = new RelayCommand(ExecuteGenerateDismissalDocument, CanExecuteEditDelete);

            // New format-specific commands
            GenerateEmploymentDocumentWordCommand = new RelayCommand(ExecuteGenerateEmploymentDocumentWord, CanExecuteEditDelete);
            GenerateEmploymentDocumentPdfCommand = new RelayCommand(ExecuteGenerateEmploymentDocumentPdf, CanExecuteEditDelete);
            GenerateDismissalDocumentWordCommand = new RelayCommand(ExecuteGenerateDismissalDocumentWord, CanExecuteEditDelete);
            GenerateDismissalDocumentPdfCommand = new RelayCommand(ExecuteGenerateDismissalDocumentPdf, CanExecuteEditDelete);

            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                EmployeesTable = await _employeeService.GetEmployeesAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Ошибка при загрузке данных: {ex.Message}");
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

        private bool CanExecuteAdminCommand(object parameter) => IsAdmin;
        private bool CanExecuteEditDelete(object parameter) => HasSelectedEmployee && IsAdmin;

        private async void ExecuteAddEmployee(object parameter)
        {
            try
            {
                var positions = await _employeeService.GetPositionsAsync();
                var newEmployee = _dialogService.ShowEmployeeCreateDialog(positions);

                if (newEmployee != null)
                {
                    await _employeeService.AddEmployeeAsync(newEmployee);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Ошибка при добавлении сотрудника: {ex.Message}");
            }
        }

        private async void ExecuteEditEmployee(object parameter)
        {
            try
            {
                if (SelectedEmployee == null) return;

                var positions = await _employeeService.GetPositionsAsync();
                var employeeData = EmployeeMapper.DataRowToEmployee(SelectedEmployee.Row);
                var updatedEmployee = _dialogService.ShowEmployeeEditDialog(employeeData, positions);

                if (updatedEmployee != null)
                {
                    await _employeeService.UpdateEmployeeAsync(updatedEmployee);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Ошибка при редактировании сотрудника: {ex.Message}");
            }
        }

        private async void ExecuteDeleteEmployee(object parameter)
        {
            try
            {
                if (SelectedEmployee == null) return;

                if (_dialogService.ShowConfirmation("Удалить сотрудника?", "Подтверждение"))
                {
                    var employeeId = (int)SelectedEmployee["EmployeeId"];
                    await _employeeService.DeleteEmployeeAsync(employeeId);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Ошибка при удалении сотрудника: {ex.Message}");
            }
        }

        private void ExecuteOpenSocial(object parameter)
        {
            try
            {
                if (parameter is DataRowView row && !string.IsNullOrEmpty(row["SocialLink"]?.ToString()))
                {
                    _employeeService.OpenSocialLink(row["SocialLink"].ToString());
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Ошибка при открытии ссылки: {ex.Message}");
            }
        }

        private void ExecuteSearch(object parameter)
        {
            FilterEmployees();
        }

        private async void ExecuteExportToCsv(object parameter)
        {
            try
            {
                var filePath = _dialogService.ShowSaveFileDialog("CSV files (*.csv)|*.csv", "Сотрудники.csv");
                if (!string.IsNullOrEmpty(filePath))
                {
                    await _exportService.ExportToCsvAsync(EmployeesTable.DefaultView, filePath);

                    if (_dialogService.ShowConfirmation("Открыть экспортированный файл?", "Открыть файл"))
                    {
                        _employeeService.OpenFile(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Ошибка при экспорте в CSV: {ex.Message}");
            }
        }

        private async void ExecuteExportToDocx(object parameter)
        {
            try
            {
                var filePath = _dialogService.ShowSaveFileDialog("Word Documents (*.docx)|*.docx",
                    $"Сотрудники_{DateTime.Now:yyyyMMdd}.docx");

                if (!string.IsNullOrEmpty(filePath))
                {
                    await _exportService.ExportToDocxAsync(EmployeesTable.DefaultView, filePath);
                    _dialogService.ShowInformation("Документ успешно экспортирован!");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Ошибка при экспорте в Word: {ex.Message}");
            }
        }

        private async void ExecuteGenerateEmploymentDocument(object parameter)
        {
            if (SelectedEmployee != null)
            {
                try
                {
                    var employee = EmployeeMapper.DataRowToEmployee(SelectedEmployee.Row);

                    System.Diagnostics.Debug.WriteLine($"Generating employment document for: {employee.FullName}, Position: {employee.Position}");

                    // Используем полное имя для enum
                    var format = _dialogService.ShowConfirmation("Создать документ в формате Word?\n(Нет - создать PDF)", "Выбор формата")
                        ? RavenHub.Services.DocumentFormat.Word
                        : RavenHub.Services.DocumentFormat.Pdf;

                    var filePath = await _documentService.GenerateEmploymentDocumentAsync(employee, format);

                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        _dialogService.ShowInformation($"Документ создан: {Path.GetFileName(filePath)}");

                        if (_dialogService.ShowConfirmation("Открыть созданный документ?", "Открыть документ"))
                        {
                            _employeeService.OpenFile(filePath);
                        }
                    }
                    else
                    {
                        _dialogService.ShowError("Не удалось создать документ");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Full error in ExecuteGenerateEmploymentDocument: {ex}");
                    _dialogService.ShowError($"Ошибка при создании документа: {ex.Message}");
                }
            }
        }

        private async void ExecuteGenerateDismissalDocument(object parameter)
        {
            if (SelectedEmployee != null)
            {
                try
                {
                    var employee = EmployeeMapper.DataRowToEmployee(SelectedEmployee.Row);

                    System.Diagnostics.Debug.WriteLine($"Generating dismissal document for: {employee.FullName}, Position: {employee.Position}");

                    // Используем полное имя для enum
                    var format = _dialogService.ShowConfirmation("Создать документ в формате Word?\n(Нет - создать PDF)", "Выбор формата")
                        ? RavenHub.Services.DocumentFormat.Word
                        : RavenHub.Services.DocumentFormat.Pdf;

                    var filePath = await _documentService.GenerateDismissalDocumentAsync(employee, format);

                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        _dialogService.ShowInformation($"Документ создан: {Path.GetFileName(filePath)}");

                        if (_dialogService.ShowConfirmation("Открыть созданный документ?", "Открыть документ"))
                        {
                            _employeeService.OpenFile(filePath);
                        }
                    }
                    else
                    {
                        _dialogService.ShowError("Не удалось создать документ");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Full error in ExecuteGenerateDismissalDocument: {ex}");
                    _dialogService.ShowError($"Ошибка при создании документа: {ex.Message}");
                }
            }
        }

        private async void ExecuteGenerateEmploymentDocumentWord(object parameter)
        {
            await GenerateDocument(RavenHub.Services.DocumentFormat.Word, true);
        }

        private async void ExecuteGenerateEmploymentDocumentPdf(object parameter)
        {
            await GenerateDocument(RavenHub.Services.DocumentFormat.Pdf, true);
        }

        private async void ExecuteGenerateDismissalDocumentWord(object parameter)
        {
            await GenerateDocument(RavenHub.Services.DocumentFormat.Word, false);
        }

        private async void ExecuteGenerateDismissalDocumentPdf(object parameter)
        {
            await GenerateDocument(RavenHub.Services.DocumentFormat.Pdf, false);
        }

        // Вспомогательный метод для генерации документов
        private async Task GenerateDocument(RavenHub.Services.DocumentFormat format, bool isEmployment)
        {
            if (SelectedEmployee != null)
            {
                try
                {
                    var employee = EmployeeMapper.DataRowToEmployee(SelectedEmployee.Row);

                    System.Diagnostics.Debug.WriteLine($"Generating {(isEmployment ? "employment" : "dismissal")} document in {format} format for: {employee.FullName}");

                    string filePath;
                    if (isEmployment)
                    {
                        filePath = await _documentService.GenerateEmploymentDocumentAsync(employee, format);
                    }
                    else
                    {
                        filePath = await _documentService.GenerateDismissalDocumentAsync(employee, format);
                    }

                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        _dialogService.ShowInformation($"Документ создан: {Path.GetFileName(filePath)}");

                        if (_dialogService.ShowConfirmation("Открыть созданный документ?", "Открыть документ"))
                        {
                            _employeeService.OpenFile(filePath);
                        }
                    }
                    else
                    {
                        _dialogService.ShowError("Не удалось создать документ");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in GenerateDocument: {ex}");
                    _dialogService.ShowError($"Ошибка при создании документа: {ex.Message}");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}