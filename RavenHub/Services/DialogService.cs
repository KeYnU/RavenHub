using Microsoft.Win32;
using RavenHub.Models;
using RavenHub.Pages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace RavenHub.Services
{
    public class DialogService : IDialogService
    {
        public void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowInformation(string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ShowConfirmation(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public string ShowSaveFileDialog(string filter, string defaultFileName)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName
            };

            return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
        }

        public Employee ShowEmployeeCreateDialog(ObservableCollection<string> positions)
        {
            // Временная реализация - возвращаем данные напрямую из окна
            // Предполагается, что у вас есть EmployeeCreateWindow
            var window = new EmployeeCreateWindow(positions);
            if (window.ShowDialog() == true)
            {
                var data = window.EmployeeData;
                return new Employee
                {
                    FullName = data["FullName"],
                    Position = data["Position"],
                    PhoneNumber = data["PhoneNumber"],
                    Email = data["Email"],
                    SocialLink = data["SocialLink"]
                };
            }
            return null;
        }

        public Employee ShowEmployeeEditDialog(Employee employee, ObservableCollection<string> positions)
        {
            var employeeData = new Dictionary<string, string>
            {
                { "Id", employee.EmployeeId.ToString() },
                { "FullName", employee.FullName },
                { "Position", employee.Position },
                { "PhoneNumber", employee.PhoneNumber },
                { "Email", employee.Email },
                { "SocialLink", employee.SocialLink }
            };

            var window = new EmployeeEditWindow(employeeData, positions);
            if (window.ShowDialog() == true)
            {
                var data = window.EmployeeData;
                return new Employee
                {
                    EmployeeId = employee.EmployeeId,
                    FullName = data["FullName"],
                    Position = data["Position"],
                    PhoneNumber = data["PhoneNumber"],
                    Email = data["Email"],
                    SocialLink = data["SocialLink"]
                };
            }
            return null;
        }
    }
}