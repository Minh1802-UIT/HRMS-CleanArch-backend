using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces
{
    public interface IExcelExportService
    {
        Task<byte[]> ExportPayrollToExcelAsync(string month);
    }
}
