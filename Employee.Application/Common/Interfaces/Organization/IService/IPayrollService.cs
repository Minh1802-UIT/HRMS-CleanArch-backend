using Employee.Domain.Common.Models;
using Employee.Application.Features.Payroll.Dtos;

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface IPayrollService
  {
    [Obsolete("Use GetPagedListAsync instead for better performance")]
    Task<IEnumerable<PayrollDto>> GetByMonthAsync(string month);

    Task<PayrollDto> GetByIdAsync(string id);
    Task<IEnumerable<PayrollDto>> GetByEmployeeIdAsync(string userId);
    Task<IEnumerable<PayrollDto>> GetMyHistoryAsync(string userId);

    // New: Pagination support
    Task<PagedResult<PayrollListDto>> GetPagedListAsync(PaginationParams pagination);
    Task<PagedResult<PayrollListDto>> GetByMonthPagedAsync(string month, PaginationParams pagination);

    /// <summary>NEW-7: Generate annual PIT report for all employees.</summary>
    Task<AnnualTaxReportDto> GetAnnualTaxReportAsync(int year);
  }
}
