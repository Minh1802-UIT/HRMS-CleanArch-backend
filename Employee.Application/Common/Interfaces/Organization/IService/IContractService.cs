using Employee.Application.Features.HumanResource.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Common.Models;

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface IContractService
  {
    Task<PagedResult<ContractDto>> GetPagedAsync(PaginationParams pagination);
    Task<ContractDto> GetByIdAsync(string id);
    Task<IEnumerable<ContractDto>> GetByEmployeeIdAsync(string employeeId);
    Task<ContractDto> CreateAsync(CreateContractDto dto);
    Task UpdateAsync(string id, UpdateContractDto contract);
    Task TerminateAsync(string id);
    Task DeleteAsync(string id);
  }
}
