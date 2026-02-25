using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces
{
  public interface IPayslipService
  {
    Task<byte[]?> GeneratePayslipPdfAsync(string payrollId);
  }
}
