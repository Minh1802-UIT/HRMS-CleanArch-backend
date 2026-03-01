using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Common.Models;

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface ILeaveTypeService
  {
    Task<PagedResult<LeaveTypeDto>> GetPagedAsync(PaginationParams pagination);
    Task<LeaveTypeDto?> GetByIdAsync(string id);
    Task CreateAsync(CreateLeaveTypeDto dto);
    Task UpdateAsync(string id, UpdateLeaveTypeDto dto);
    Task DeleteAsync(string id);
  }
}
