using System.Collections.ObjectModel;
using RavenHub.Models;

namespace RavenHub.Services
{
    public interface IDialogService
    {
        void ShowError(string message);
        void ShowInformation(string message);
        bool ShowConfirmation(string message, string title);
        string ShowSaveFileDialog(string filter, string defaultFileName);
        Employee ShowEmployeeCreateDialog(ObservableCollection<string> positions);
        Employee ShowEmployeeEditDialog(Employee employee, ObservableCollection<string> positions);
    }
}