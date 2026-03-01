using Employee.Application.Common.Interfaces.Attendance.IService;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Features.Attendance.Mappers;
using Employee.Domain.Common.Models;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Entities.Attendance;


namespace Employee.Application.Features.Attendance.Services
{
  public class ShiftService : IShiftService
  {
    private readonly IShiftRepository _shiftRepository;

    public ShiftService(IShiftRepository shiftRepository)
    {
      _shiftRepository = shiftRepository;
    }

    public async Task<PagedResult<ShiftDto>> GetPagedAsync(PaginationParams pagination)
    {
      var paged = await _shiftRepository.GetPagedAsync(pagination);
      return new PagedResult<ShiftDto>
      {
        Items = paged.Items.Select(x => x.ToDto()).ToList(),
        TotalCount = paged.TotalCount,
        PageNumber = paged.PageNumber,
        PageSize = paged.PageSize
      };
    }

    public async Task<ShiftDto?> GetByIdAsync(string id)
    {
      var entity = await _shiftRepository.GetByIdAsync(id);
      return entity?.ToDto();
    }

    public async Task<string> CreateAsync(CreateShiftDto dto)
    {
      // Validate trùng Code
      var existing = await _shiftRepository.GetByCodeAsync(dto.Code);
      if (existing != null)
      {
        throw new ConflictException($"Shift code '{dto.Code}' already exists.");
      }

      // Convert DTO -> Entity
      var entity = dto.ToEntity();

      await _shiftRepository.CreateAsync(entity);
      return entity.Id;
    }

    public async Task UpdateAsync(string id, UpdateShiftDto dto)
    {
      var entity = await _shiftRepository.GetByIdAsync(id);
      if (entity == null) throw new NotFoundException("Shift not found");

      // Update Entity từ DTO
      entity.UpdateFromDto(dto);

      await _shiftRepository.UpdateAsync(id, entity);
    }

    public async Task DeleteAsync(string id)
    {
      await _shiftRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<ShiftLookupDto>> GetLookupAsync()
    {
      var activeShifts = await _shiftRepository.GetAllActiveAsync();

      return activeShifts
          .Where(s => s.IsActive)
          .Select(s => new ShiftLookupDto
          {
            Id = s.Id,
            Name = s.Name,
            Code = s.Code,
            TimeRange = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"
          });
    }
  }
}
