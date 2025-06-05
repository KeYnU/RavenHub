using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using RavenHub.Models;

namespace RavenHub.Services
{
    public interface IEmployeeService
    {
        Task<DataTable> GetEmployeesAsync();
        Task<ObservableCollection<string>> GetPositionsAsync();
        Task AddEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int employeeId);
        void OpenSocialLink(string link);
        void OpenFile(string filePath);
    }
}