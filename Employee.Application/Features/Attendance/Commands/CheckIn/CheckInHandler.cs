using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Attendance.Mappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Attendance.Commands.CheckIn
{
  /// <summary>
  /// Saves the raw punch log then immediately triggers processing so the
  /// attendance bucket is updated in real-time. The background job continues to
  /// sweep any logs that slipped through (e.g. service restart mid-request).
  /// </summary>
  public class CheckInHandler : IRequestHandler<CheckInCommand>
  {
    private readonly IRawAttendanceLogRepository _rawRepo;
    private readonly IAttendanceProcessingService _processingService;
    private readonly ILogger<CheckInHandler> _logger;

    public CheckInHandler(
        IRawAttendanceLogRepository rawRepo,
        IAttendanceProcessingService processingService,
        ILogger<CheckInHandler> logger)
    {
      _rawRepo           = rawRepo;
      _processingService = processingService;
      _logger            = logger;
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

      // 3. Persist the raw punch
      await _rawRepo.CreateAsync(rawLog);

      // 4. Process immediately so the attendance bucket reflects the punch in real-time.
      //    The atomic lock inside ProcessRawLogsAsync prevents double-processing with
      //    the background job sweep.
      try
      {
        await _processingService.ProcessRawLogsAsync();
      }
      catch (Exception ex)
      {
        // Never fail the check-in request because of processing errors.
        // The background job will retry on its next sweep.
        _logger.LogWarning(ex,
            "CheckInHandler: inline processing failed for EmployeeId={Id}. " +
            "Background job will retry. Error: {Error}",
            request.EmployeeId, ex.Message);
      }
    }
  }
}
