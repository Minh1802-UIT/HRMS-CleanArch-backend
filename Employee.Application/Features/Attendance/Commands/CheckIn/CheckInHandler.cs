using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Exceptions; // Add this
using Employee.Application.Features.Attendance.Mappers;
using MediatR;

namespace Employee.Application.Features.Attendance.Commands.CheckIn
{
  public class CheckInHandler : IRequestHandler<CheckInCommand>
  {
    private readonly IRawAttendanceLogRepository _rawRepo;

    public CheckInHandler(IRawAttendanceLogRepository rawRepo)
    {
      _rawRepo = rawRepo;
    }

    public async Task Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
      // 1. SPAM PROTECTION: Chống thao tác quá nhanh (60s)
      var latestLog = await _rawRepo.GetLatestLogAsync(request.EmployeeId);
      if (latestLog != null)
      {
        var diff = DateTime.UtcNow - latestLog.Timestamp;
        if (diff.TotalSeconds < 60)
        {
          throw new Employee.Application.Common.Exceptions.ConflictException(
              $"Bạn thao tác quá nhanh! Vui lòng chờ {60 - (int)diff.TotalSeconds} giây nữa.");
        }
      }

      // 2. Map DTO -> Entity Raw Log
      // Mapper này đã có ở bước trước
      var rawLog = request.Dto.ToRawEntity(request.EmployeeId);

      // 3. Lưu vào bảng RawAttendanceLog
      await _rawRepo.CreateAsync(rawLog);

      // Tùy chọn: Xử lý chấm công Real-time nếu cần
    }
  }
}
