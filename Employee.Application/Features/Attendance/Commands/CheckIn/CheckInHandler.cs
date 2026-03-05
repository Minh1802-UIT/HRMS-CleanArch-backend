using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Attendance.Mappers;
using MediatR;

namespace Employee.Application.Features.Attendance.Commands.CheckIn
{
  /// <summary>
  /// Saves the raw punch log and returns immediately.
  /// All heavy processing is handled asynchronously by
  /// <c>AttendanceProcessingBackgroundJob</c> (runs every 5 minutes).
  /// </summary>
  public class CheckInHandler : IRequestHandler<CheckInCommand>
  {
    private readonly IRawAttendanceLogRepository _rawRepo;

    public CheckInHandler(IRawAttendanceLogRepository rawRepo)
    {
      _rawRepo = rawRepo;
    }

    public async Task Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
      // 1. SPAM PROTECTION: block actions faster than 60 seconds apart
      var latestLog = await _rawRepo.GetLatestLogAsync(request.EmployeeId);
      if (latestLog != null)
      {
        var diff = DateTime.UtcNow - latestLog.Timestamp;
        if (diff.TotalSeconds < 60)
        {
          throw new ConflictException(
              $"Bạn thao tác quá nhanh! Vui lòng chờ {60 - (int)diff.TotalSeconds} giây nữa.");
        }
      }

      // 2. Map DTO → entity (timestamp captured as UTC here)
      var rawLog = request.Dto.ToRawEntity(request.EmployeeId);

      // 3. Persist the raw punch — background job will pick it up within 5 minutes
      await _rawRepo.CreateAsync(rawLog);
    }
  }
}
