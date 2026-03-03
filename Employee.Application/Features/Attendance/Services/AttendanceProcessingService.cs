using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Attendance.Logic;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Attendance.Services
{
  public class AttendanceProcessingService : IAttendanceProcessingService
  {
    private readonly IRawAttendanceLogRepository _rawRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IShiftRepository _shiftRepo;
    private readonly AttendanceCalculator _calculator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AttendanceProcessingService> _logger;

    // Cấu hình Timezone: UTC+7
    private readonly TimeSpan _systemOffset = TimeSpan.FromHours(7);

    public AttendanceProcessingService(
        IRawAttendanceLogRepository rawRepo,
        IAttendanceRepository attendanceRepo,
        IEmployeeRepository employeeRepo,
        IShiftRepository shiftRepo,
        AttendanceCalculator calculator,
        IUnitOfWork unitOfWork,
        ILogger<AttendanceProcessingService> logger)
    {
      _rawRepo = rawRepo;
      _attendanceRepo = attendanceRepo;
      _employeeRepo = employeeRepo;
      _shiftRepo = shiftRepo;
      _calculator = calculator;
      _unitOfWork = unitOfWork;
      _logger = logger;
    }

    public async Task<string> ProcessRawLogsAsync()
    {
      try
      {
        // 1. CONCURRENCY: Lấy và Lock dữ liệu để tránh race condition
        var logs = await _rawRepo.GetAndLockUnprocessedLogsAsync(50);
        if (!logs.Any())
        {
          _logger.LogDebug("ProcessRawLogsAsync: No unprocessed logs found.");
          return "Found 0 unprocessed logs in DB. Please check if raw_attendance_logs collection has any record with IsProcessed: false.";
        }

        _logger.LogInformation("ProcessRawLogsAsync: Found {Count} unprocessed logs. Processing...", logs.Count);

        int processedCount = 0;
        int failedCount = 0;

        // 2. Group theo ngày Local Time
        var groupedLogs = logs.GroupBy(x => new
        {
          x.EmployeeId,
          Date = (x.Timestamp + _systemOffset).Date
        });

        foreach (var group in groupedLogs)
        {
          try
          {
            await ProcessSingleGroupAsync(group.Key.EmployeeId, group.Key.Date, group.ToList());
            processedCount += group.Count();
            _logger.LogInformation("ProcessRawLogsAsync: Processed {Count} logs for EmployeeId={EmployeeId} Date={Date}", group.Count(), group.Key.EmployeeId, group.Key.Date.ToString("yyyy-MM-dd"));
          }
          catch (Exception ex)
          {
            failedCount += group.Count();
            _logger.LogError(ex, "ProcessRawLogsAsync: FAILED processing group EmployeeId={EmployeeId} Date={Date}. Error: {Error}", group.Key.EmployeeId, group.Key.Date.ToString("yyyy-MM-dd"), ex.Message);
            await MarkGroupAsError(group, ex.Message);
          }
        }

        var result = $"Processed {processedCount} logs successfully. {failedCount} logs failed. Check bucket collection.";
        _logger.LogInformation("ProcessRawLogsAsync: {Result}", result);
        return result;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "ProcessRawLogsAsync: CRITICAL ERROR. {Error}", ex.Message);
        return $"CRITICAL ERROR in Processing Service: {ex.Message}";
      }
    }

    private async Task ProcessSingleGroupAsync(string employeeId, DateTime workDate, List<RawAttendanceLog> newLogs)
    {
      await _unitOfWork.BeginTransactionAsync();
      try
      {
        var monthKey = workDate.ToString("MM-yyyy");

        // --- BƯỚC A: XỬ LÝ GHOST LOG ---
        await ProcessGhostLogAsync(employeeId, workDate.AddDays(-1));

        // --- BƯỚC B: CHUẨN BỊ DỮ LIỆU ---
        var shift = await GetEffectiveShiftAsync(employeeId, workDate);
        var bucket = await GetOrCreateBucketAsync(employeeId, monthKey);

        var dailyLog = bucket.DailyLogs.FirstOrDefault(x => x.Date.Date == workDate.Date);
        if (dailyLog == null)
        {
          dailyLog = new DailyLog(workDate, AttendanceStatus.Absent);
          bucket.AddOrUpdateDailyLog(dailyLog);
        }

        // --- BƯỚC C: MERGE LOG ---
        var checkInLogs = newLogs.Where(x => x.Type == RawLogType.CheckIn).Select(x => x.Timestamp).ToList();
        var checkOutLogs = newLogs.Where(x => x.Type == RawLogType.CheckOut).Select(x => x.Timestamp).ToList();

        DateTime? checkIn = dailyLog.CheckIn;
        DateTime? checkOut = dailyLog.CheckOut;

        if (checkInLogs.Any())
        {
          var newMin = checkInLogs.Min();
          checkIn = checkIn.HasValue ? (newMin < checkIn.Value ? newMin : checkIn) : newMin;
        }

        if (checkOutLogs.Any())
        {
          var newMax = checkOutLogs.Max();
          checkOut = checkOut.HasValue ? (newMax > checkOut.Value ? newMax : checkOut) : newMax;
        }

        var biometricLogs = newLogs.Where(x => x.Type == RawLogType.Biometric).Select(x => x.Timestamp).ToList();
        if (biometricLogs.Any())
        {
          var allTimes = biometricLogs.OrderBy(x => x).ToList();
          if (!checkIn.HasValue) checkIn = allTimes.First();
          checkOut = allTimes.Last();
        }

        dailyLog.UpdateCheckTimes(checkIn, checkOut, shift?.Code ?? "Unknown");

        // --- BƯỚC D: TÍNH TOÁN ---
        _calculator.CalculateDailyStatus(dailyLog, shift);

        // --- BƯỚC E: LƯU ---
        bucket.AddOrUpdateDailyLog(dailyLog); // Re-add to ensure recalculated totals
        await _attendanceRepo.UpdateAsync(bucket.Id, bucket);

        // Mark all logs in this group as processed with a single BulkWriteAsync
        // (replaces the N individual UpdateOneAsync round-trips that were here before).
        foreach (var log in newLogs)
          log.MarkAsProcessed(); // domain state update (in-memory only)

        await _rawRepo.MarkManyAsProcessedAsync(newLogs.Select(l => l.Id));

        await _unitOfWork.CommitTransactionAsync();
      }
      catch (Exception)
      {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
      }
    }

    private async Task ProcessGhostLogAsync(string employeeId, DateTime prevDate)
    {
      var prevMonthKey = prevDate.ToString("MM-yyyy");
      var bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(employeeId, prevMonthKey);
      if (bucket == null) return;

      var prevLog = bucket.DailyLogs.FirstOrDefault(x => x.Date.Date == prevDate.Date);
      if (prevLog != null && prevLog.CheckIn.HasValue && !prevLog.CheckOut.HasValue)
      {
        var prevShift = await GetEffectiveShiftAsync(employeeId, prevDate);
        if (prevShift != null)
        {
          var shiftEndDateTime = prevDate.Add(prevShift.EndTime);
          if (prevShift.IsOvernight) shiftEndDateTime = shiftEndDateTime.AddDays(1);

          var autoCheckOut = shiftEndDateTime - _systemOffset;
          prevLog.UpdateCheckTimes(prevLog.CheckIn, autoCheckOut, prevShift.Code);
          prevLog.UpdateCalculationResults(0, 0, 0, 0, AttendanceStatus.Absent, "Missing Checkout [System Auto-closed]");

          _calculator.CalculateDailyStatus(prevLog, prevShift);
          bucket.AddOrUpdateDailyLog(prevLog);
          await _attendanceRepo.UpdateAsync(bucket.Id, bucket);
        }
      }
    }

    private async Task<Shift?> GetEffectiveShiftAsync(string employeeId, DateTime date)
    {
      var rosterShift = await _shiftRepo.GetShiftByDateAsync(employeeId, date);
      if (rosterShift != null) return rosterShift;

      var employee = await _employeeRepo.GetByIdAsync(employeeId);
      if (!string.IsNullOrEmpty(employee?.JobDetails.ShiftId))
      {
        return await _shiftRepo.GetByIdAsync(employee.JobDetails.ShiftId);
      }
      return null;
    }

    private async Task<AttendanceBucket> GetOrCreateBucketAsync(string employeeId, string monthKey)
    {
      var bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(employeeId, monthKey);
      if (bucket == null)
      {
        bucket = new AttendanceBucket(employeeId, monthKey);
        await _attendanceRepo.CreateAsync(bucket);
        bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(employeeId, monthKey);
      }
      return bucket!;
    }

    private async Task MarkGroupAsError(IEnumerable<RawAttendanceLog> group, string error)
    {
      foreach (var log in group)
      {
        log.MarkAsFailed(error);
        await _rawRepo.MarkAsErrorAsync(log.Id, error);
      }
    }
  }
}
