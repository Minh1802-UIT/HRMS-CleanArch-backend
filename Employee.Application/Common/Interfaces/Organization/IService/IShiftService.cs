using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Common.Models;

namespace Employee.Application.Common.Interfaces.Attendance.IService
{
  public interface IShiftService
  {
    Task<PagedResult<ShiftDto>> GetPagedAsync(PaginationParams pagination);
    Task<ShiftDto?> GetByIdAsync(string id);
    Task<string> CreateAsync(CreateShiftDto dto);
    Task UpdateAsync(string id, UpdateShiftDto dto);
    Task DeleteAsync(string id);

    // New: Lookup for Dropdowns
    Task<IEnumerable<ShiftLookupDto>> GetLookupAsync();
  }
}