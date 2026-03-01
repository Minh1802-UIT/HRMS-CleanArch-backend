using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Leave.Dtos;
using Employee.Application.Features.Leave.Mappers;
using MediatR;

namespace Employee.Application.Features.Leave.Queries.GetLeaveRequestById
{
  public class GetLeaveRequestByIdQueryHandler : IRequestHandler<GetLeaveRequestByIdQuery, LeaveRequestDto>
  {
    private readonly ILeaveRequestRepository _repo;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveTypeRepository _typeRepo;
    private readonly ICurrentUser _currentUser;

    public GetLeaveRequestByIdQueryHandler(
        ILeaveRequestRepository repo,
        IEmployeeRepository empRepo,
        ILeaveTypeRepository typeRepo,
        ICurrentUser currentUser)
    {
      _repo = repo;
      _empRepo = empRepo;
      _typeRepo = typeRepo;
      _currentUser = currentUser;
    }

    public async Task<LeaveRequestDto> Handle(GetLeaveRequestByIdQuery request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Leave request with ID {request.Id} not found.");

      // Ownership check: employees can only view their own requests.
      // Admin / HR / Manager can view any request.
      var isPrivileged = _currentUser.IsInRole("Admin") ||
                         _currentUser.IsInRole("HR") ||
                         _currentUser.IsInRole("Manager");

      if (!isPrivileged && entity.EmployeeId != (_currentUser.EmployeeId ?? _currentUser.UserId))
        throw new ForbiddenException("You do not have permission to view this leave request.");

      var emp = await _empRepo.GetByIdAsync(entity.EmployeeId, cancellationToken);
      var name = emp?.FullName ?? "Unknown";
      var code = emp?.EmployeeCode ?? "Unknown";

      var leaveTypeCode = entity.LeaveType.ToString();
      var leaveType = await _typeRepo.GetByCodeAsync(leaveTypeCode, cancellationToken);
      var typeName = leaveType?.Name ?? leaveTypeCode;

      return entity.ToDto(name, code, leaveTypeName: typeName);
    }
  }
}
