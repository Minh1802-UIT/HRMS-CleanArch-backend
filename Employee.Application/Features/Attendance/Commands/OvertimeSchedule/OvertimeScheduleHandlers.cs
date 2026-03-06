using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using MediatR;

namespace Employee.Application.Features.Attendance.Commands.OvertimeSchedule
{
  // ── CREATE (single) ───────────────────────────────────────────────────────

  public class CreateOvertimeScheduleCommand : IRequest<OvertimeScheduleDto>
  {
    public CreateOvertimeScheduleDto Dto { get; set; } = null!;
  }

  public class CreateOvertimeScheduleHandler
      : IRequestHandler<CreateOvertimeScheduleCommand, OvertimeScheduleDto>
  {
    private readonly IOvertimeScheduleRepository _repo;

    public CreateOvertimeScheduleHandler(IOvertimeScheduleRepository repo) => _repo = repo;

    public async Task<OvertimeScheduleDto> Handle(
        CreateOvertimeScheduleCommand request, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(request.Dto.EmployeeId))
        throw new ValidationException("EmployeeId is required.");

      // Prevent duplicates
      var exists = await _repo.ExistsAsync(request.Dto.EmployeeId, request.Dto.Date, cancellationToken);
      if (exists)
        throw new ConflictException(
            $"Ngày {request.Dto.Date:dd/MM/yyyy} đã được đăng ký OT cho nhân viên này.");

      var entry = new Domain.Entities.Attendance.OvertimeSchedule(
          request.Dto.EmployeeId, request.Dto.Date, request.Dto.Note);
      await _repo.CreateAsync(entry);
      return entry.ToDto();
    }
  }

  // ── CREATE BULK ───────────────────────────────────────────────────────────

  public class CreateBulkOvertimeScheduleCommand : IRequest<List<OvertimeScheduleDto>>
  {
    public CreateBulkOvertimeScheduleDto Dto { get; set; } = null!;
  }

  public class CreateBulkOvertimeScheduleHandler
      : IRequestHandler<CreateBulkOvertimeScheduleCommand, List<OvertimeScheduleDto>>
  {
    private readonly IOvertimeScheduleRepository _repo;

    public CreateBulkOvertimeScheduleHandler(IOvertimeScheduleRepository repo) => _repo = repo;

    public async Task<List<OvertimeScheduleDto>> Handle(
        CreateBulkOvertimeScheduleCommand request, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(request.Dto.EmployeeId))
        throw new ValidationException("EmployeeId is required.");
      if (!request.Dto.Dates.Any())
        throw new ValidationException("At least one date is required.");

      var results = new List<OvertimeScheduleDto>();
      foreach (var date in request.Dto.Dates.Distinct())
      {
        var exists = await _repo.ExistsAsync(request.Dto.EmployeeId, date, cancellationToken);
        if (exists) continue; // skip duplicates silently

        var entry = new Domain.Entities.Attendance.OvertimeSchedule(
            request.Dto.EmployeeId, date, request.Dto.Note);
        await _repo.CreateAsync(entry);
        results.Add(entry.ToDto());
      }
      return results;
    }
  }

  // ── DELETE ────────────────────────────────────────────────────────────────

  public class DeleteOvertimeScheduleCommand : IRequest<Unit>
  {
    public string Id { get; set; } = string.Empty;
  }

  public class DeleteOvertimeScheduleHandler
      : IRequestHandler<DeleteOvertimeScheduleCommand, Unit>
  {
    private readonly IOvertimeScheduleRepository _repo;

    public DeleteOvertimeScheduleHandler(IOvertimeScheduleRepository repo) => _repo = repo;

    public async Task<Unit> Handle(
        DeleteOvertimeScheduleCommand request, CancellationToken cancellationToken)
    {
      var entry = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (entry == null)
        throw new NotFoundException($"OvertimeSchedule '{request.Id}' not found.");

      await _repo.DeleteAsync(request.Id, cancellationToken);
      return Unit.Value;
    }
  }

  // ── QUERY: LIST BY MONTH ──────────────────────────────────────────────────

  public class GetOvertimeSchedulesByMonthQuery : IRequest<List<OvertimeScheduleDto>>
  {
    /// <summary>"MM-yyyy"</summary>
    public string MonthKey { get; set; } = string.Empty;
    /// <summary>Filter by employee (optional; admin sees all if null).</summary>
    public string? EmployeeId { get; set; }
  }

  public class GetOvertimeSchedulesByMonthHandler
      : IRequestHandler<GetOvertimeSchedulesByMonthQuery, List<OvertimeScheduleDto>>
  {
    private readonly IOvertimeScheduleRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetOvertimeSchedulesByMonthHandler(
        IOvertimeScheduleRepository repo,
        IEmployeeRepository employeeRepo)
    {
      _repo = repo;
      _employeeRepo = employeeRepo;
    }

    public async Task<List<OvertimeScheduleDto>> Handle(
        GetOvertimeSchedulesByMonthQuery request, CancellationToken cancellationToken)
    {
      List<Domain.Entities.Attendance.OvertimeSchedule> entries;

      if (!string.IsNullOrWhiteSpace(request.EmployeeId))
        entries = await _repo.GetByEmployeeAndMonthAsync(request.EmployeeId, request.MonthKey, cancellationToken);
      else
        entries = await _repo.GetByMonthAsync(request.MonthKey, cancellationToken);

      // Enrich with employee names (batch load unique IDs)
      var employeeIds = entries.Select(e => e.EmployeeId).Distinct().ToList();
      var nameMap = new Dictionary<string, string?>();
      foreach (var eId in employeeIds)
      {
        var emp = await _employeeRepo.GetByIdAsync(eId);
        nameMap[eId] = emp?.FullName;
      }

      return entries.Select(e => e.ToDto(nameMap.GetValueOrDefault(e.EmployeeId))).ToList();
    }
  }
}

// ── MAPPER ────────────────────────────────────────────────────────────────

namespace Employee.Application.Features.Attendance.Commands.OvertimeSchedule
{
  internal static class OvertimeScheduleMapper
  {
    public static Dtos.OvertimeScheduleDto ToDto(
        this Domain.Entities.Attendance.OvertimeSchedule e, string? employeeName = null)
        => new()
        {
          Id           = e.Id,
          EmployeeId   = e.EmployeeId,
          EmployeeName = employeeName,
          Date         = e.Date,
          Note         = e.Note,
          CreatedAt    = e.CreatedAt,
        };
  }
}
