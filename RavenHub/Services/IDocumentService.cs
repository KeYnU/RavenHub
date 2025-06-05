using System.Threading.Tasks;
using RavenHub.Models;

namespace RavenHub.Services
{
    public interface IDocumentService
    {
        Task<string> GenerateEmploymentDocumentAsync(Employee employee, DocumentFormat format);
        Task<string> GenerateDismissalDocumentAsync(Employee employee, DocumentFormat format);
    }

    public enum DocumentFormat
    {
        Word,
        Pdf
    }
}