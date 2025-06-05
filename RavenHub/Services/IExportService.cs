using System.Data;
using System.Threading.Tasks;

namespace RavenHub.Services
{
    public interface IExportService
    {
        Task ExportToCsvAsync(DataView dataView, string filePath);
        Task ExportToDocxAsync(DataView dataView, string filePath);
    }
}