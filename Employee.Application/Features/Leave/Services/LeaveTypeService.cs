using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Common.Models;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Leave.Dtos;
using Employee.Application.Features.Leave.Mappers;

namespace Employee.Application.Features.Leave.Services
{
  public class LeaveTypeService : ILeaveTypeService
  {
    private readonly ILeaveTypeRepository _repo;

    public LeaveTypeService(ILeaveTypeRepository repo)
    {
      _repo = repo;
    }

    public async Task<PagedResult<LeaveTypeDto>> GetPagedAsync(PaginationParams pagination)
    {
      var paged = await _repo.GetPagedAsync(pagination);
      return new PagedResult<LeaveTypeDto>
      {
        Items = paged.Items.Select(x => x.ToDto()).ToList(),
        TotalCount = paged.TotalCount,
        PageNumber = paged.PageNumber,
        PageSize = paged.PageSize
      };
    }

    public async Task<LeaveTypeDto?> GetByIdAsync(string id)
    {
      var entity = await _repo.GetByIdAsync(id);
      return entity?.ToDto();
    }

    public async Task CreateAsync(CreateLeaveTypeDto dto)
    {
      var entity = dto.ToEntity();
      await _repo.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, UpdateLeaveTypeDto dto)
    {
      var entity = await _repo.GetByIdAsync(id);
      if (entity == null) throw new NotFoundException("Leave Type not found");

      entity.UpdateFromDto(dto);
      await _repo.UpdateAsync(id, entity);
    }

    public async Task DeleteAsync(string id) => await _repo.DeleteAsync(id);
  }
}
