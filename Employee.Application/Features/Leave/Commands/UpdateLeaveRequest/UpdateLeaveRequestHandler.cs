using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using MediatR;

namespace Employee.Application.Features.Leave.Commands.UpdateLeaveRequest
{
  public class UpdateLeaveRequestHandler : IRequestHandler<UpdateLeaveRequestCommand>
  {
    private readonly ILeaveRequestRepository _repo;

    public UpdateLeaveRequestHandler(ILeaveRequestRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(UpdateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (entity == null) throw new NotFoundException($"Không tìm thấy đơn nghỉ phép có ID '{request.Id}'");

      if (entity.EmployeeId != request.EmployeeId)
        throw new ValidationException("Bạn không có quyền sửa đơn này");

      // Parse Enum
      if (!Enum.TryParse<Employee.Domain.Enums.LeaveTypeEnum>(request.Dto.LeaveType, true, out var leaveTypeEnum))
      {
        throw new ValidationException($"Loại nghỉ phép '{request.Dto.LeaveType}' không hợp lệ.");
      }

      // Check date overlap — pass excludeId to avoid flagging this request as competing with itself
      var hasOverlap = await _repo.ExistsOverlapAsync(entity.EmployeeId, request.Dto.FromDate, request.Dto.ToDate, request.Id, cancellationToken);
      if (hasOverlap)
      {
        throw new ValidationException("Khoảng thời gian này bạn đã có đơn nghỉ phép khác.");
      }

      try
      {
        entity.Update(leaveTypeEnum, request.Dto.FromDate, request.Dto.ToDate, request.Dto.Reason);
      }
      catch (InvalidOperationException ex)
      {
        throw new ValidationException(ex.Message);
      }

      await _repo.UpdateAsync(request.Id, entity, cancellationToken);
    }
  }
}
