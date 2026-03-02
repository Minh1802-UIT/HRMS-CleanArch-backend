using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Attendance.Mappers;
using MediatR;

namespace Employee.Application.Features.Attendance.Commands.CheckIn
{
  public class CheckInHandler : IRequestHandler<CheckInCommand>
  {
    private readonly IRawAttendanceLogRepository _rawRepo;
    private readonly IAttendanceProcessingService _processingService;

    public CheckInHandler(
      IRawAttendanceLogRepository rawRepo,
      IAttendanceProcessingService processingService)
    {
      _rawRepo = rawRepo;
      _processingService = processingService;
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
          throw new ConflictException(
              $"Bạn thao tác quá nhanh! Vui lòng chờ {60 - (int)diff.TotalSeconds} giây nữa.");
        }
      }

      // 2. Map DTO -> Entity
      var rawLog = request.Dto.ToRawEntity(request.EmployeeId);

      // 3. Lưu RawAttendanceLog
      await _rawRepo.CreateAsync(rawLog);

      // 4. Xử lý real-time: cập nhật AttendanceBucket ngay lập tức
      //    Để My History phản ánh check-in ngay sau khi submit
      await _processingService.ProcessRawLogsAsync();
    }
  }
}
