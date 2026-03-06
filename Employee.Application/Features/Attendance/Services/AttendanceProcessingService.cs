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
    private readonly IPublicHolidayRepository _holidayRepo;
    private readonly IOvertimeScheduleRepository _otScheduleRepo;
    private readonly AttendanceCalculator _calculator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AttendanceProcessingService> _logger;
    private readonly TimeZoneInfo _timeZone;

    // Logical-Day cut-off hour (local time).
    // Any punch timestamp whose local hour is less than this value is assigned to the
    // PREVIOUS calendar day. This lets overnight-shift checkouts (e.g. 05:30 AM of D+1)
    // stay in the same logical work-day as the CheckIn from D evening. (BUG-01 fix)
    private const int LogicalDayCutoffHour = 6;

    public AttendanceProcessingService(
        IRawAttendanceLogRepository rawRepo,
        IAttendanceRepository attendanceRepo,
        IEmployeeRepository employeeRepo,
        IShiftRepository shiftRepo,
        IPublicHolidayRepository holidayRepo,
        IOvertimeScheduleRepository otScheduleRepo,
        AttendanceCalculator calculator,
        IUnitOfWork unitOfWork,
        ILogger<AttendanceProcessingService> logger,
        TimeZoneInfo timeZone)
    {
      _rawRepo = rawRepo;
      _attendanceRepo = attendanceRepo;
      _employeeRepo = employeeRepo;
      _shiftRepo = shiftRepo;
      _holidayRepo = holidayRepo;
      _otScheduleRepo = otScheduleRepo;
      _calculator = calculator;
      _unitOfWork = unitOfWork;
      _logger = logger;
      _timeZone = timeZone;
    }

    // -------------------------------------------------------------------------
    // Public entry-point: called by the background job every 5 minutes
    // -------------------------------------------------------------------------
    public async Task<string> ProcessRawLogsAsync()
    {
      try
      {
        var logs = await _rawRepo.GetAndLockUnprocessedLogsAsync(50);
        if (!logs.Any())
        {
          _logger.LogDebug("ProcessRawLogsAsync: No unprocessed logs found.");
          return "Found 0 unprocessed logs in DB.";
        }

        _logger.LogInformation("ProcessRawLogsAsync: Found {Count} unprocessed logs. Processing...", logs.Count);

        // --- Guard: permanently discard records whose Timestamp is obviously invalid
        //     (year < 2000 means DateTime.MinValue or corrupted data).
        //     We set IsProcessed=true so they are never retried.
        var invalidLogs = logs.Where(x => x.Timestamp.Year < 2000).ToList();
        if (invalidLogs.Any())
        {
          _logger.LogWarning(
              "ProcessRawLogsAsync: Discarding {Count} raw log(s) with invalid Timestamp (year < 2000). IDs: {Ids}",
              invalidLogs.Count,
              string.Join(", ", invalidLogs.Select(x => x.Id)));
          await _rawRepo.MarkManyAsPermanentErrorAsync(
              invalidLogs.Select(x => x.Id),
              "INVALID_TIMESTAMP");
          logs = logs.Where(x => x.Timestamp.Year >= 2000).ToList();
          if (!logs.Any())
            return $"Found 0 valid logs to process (discarded {invalidLogs.Count} with invalid timestamps).";
        }

        int processedCount = 0;
        int failedCount    = 0;

        // Group by (EmployeeId + LogicalDate) — BUG-01: use day-breaker, NOT raw LocalDate
        var groupedLogs = logs.GroupBy(x => new
        {
          x.EmployeeId,
          LogicalDate = GetLogicalDate(x.Timestamp)
        }).ToList();

        // Load public holidays covering all logical dates in this batch (one round-trip)
        var batchDates = groupedLogs.Select(g => g.Key.LogicalDate).Distinct().ToList();
        var batchStart = batchDates.Min();
        var batchEnd = batchDates.Max();
        var holidayList = await _holidayRepo.GetByDateRangeAsync(batchStart, batchEnd);
        var holidayMap = holidayList.ToDictionary(h => h.Date.Date, h => h.Name);

        foreach (var group in groupedLogs)
        {
          try
          {
            await ProcessSingleGroupAsync(group.Key.EmployeeId, group.Key.LogicalDate, group.ToList(), holidayMap);
            processedCount += group.Count();
            _logger.LogInformation(
                "ProcessRawLogsAsync: Processed {Count} logs for EmployeeId={EmployeeId} LogicalDate={Date}",
                group.Count(), group.Key.EmployeeId, group.Key.LogicalDate.ToString("yyyy-MM-dd"));
          }
          catch (Exception ex)
          {
            failedCount += group.Count();
            _logger.LogError(ex,
                "ProcessRawLogsAsync: FAILED processing group EmployeeId={EmployeeId} LogicalDate={Date}. Error: {Error}",
                group.Key.EmployeeId, group.Key.LogicalDate.ToString("yyyy-MM-dd"), ex.Message);
            await MarkGroupAsError(group, ex.Message);
          }
        }

        var result = $"Processed {processedCount} logs successfully. {failedCount} logs failed.";
        _logger.LogInformation("ProcessRawLogsAsync: {Result}", result);
        return result;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "ProcessRawLogsAsync: CRITICAL ERROR. {Error}", ex.Message);
        return $"CRITICAL ERROR in Processing Service: {ex.Message}";
      }
    }

    // -------------------------------------------------------------------------
    // Core: process one (employeeId + logicalDate) group
    // -------------------------------------------------------------------------
    private async Task ProcessSingleGroupAsync(
        string employeeId, DateTime workDate, List<RawAttendanceLog> newLogs,
        IReadOnlyDictionary<DateTime, string>? holidayMap = null)
    {
      await _unitOfWork.BeginTransactionAsync();
      try
      {
        var monthKey = workDate.ToString("MM-yyyy");

        // --- STEP A: Resolve Ghost Log of the previous logical day ---
        await ProcessGhostLogAsync(employeeId, workDate.AddDays(-1));

        // --- STEP B: Resolve effective shift & bucket ---
        var shift  = await GetEffectiveShiftAsync(employeeId, workDate);
        var bucket = await GetOrCreateBucketAsync(employeeId, monthKey);

        var dailyLog = bucket.DailyLogs.FirstOrDefault(x => x.Date.Date == workDate.Date);
        if (dailyLog == null)
        {
          dailyLog = DailyLog.Create(workDate, AttendanceStatus.Absent);
          bucket.AddOrUpdateDailyLog(dailyLog);
        }

        // --- STEP C: Merge new raw logs with existing check-times (idempotent) ---
        var checkInLogs  = newLogs.Where(x => x.Type == RawLogType.CheckIn).Select(x => x.Timestamp).ToList();
        var checkOutLogs = newLogs.Where(x => x.Type == RawLogType.CheckOut).Select(x => x.Timestamp).ToList();

        DateTime? checkIn  = dailyLog.CheckIn;
        DateTime? checkOut = dailyLog.CheckOut;

        // Explicit CheckIn: keep the earliest punch of the day
        if (checkInLogs.Any())
        {
          var newMin = checkInLogs.Min();
          checkIn = checkIn.HasValue ? (newMin < checkIn.Value ? newMin : checkIn) : newMin;
        }

        // Explicit CheckOut: keep the latest punch of the day
        if (checkOutLogs.Any())
        {
          var newMax = checkOutLogs.Max();
          checkOut = checkOut.HasValue ? (newMax > checkOut.Value ? newMax : checkOut) : newMax;
        }

        // Biometric: first punch = CheckIn (if unknown), last punch = CheckOut ONLY if later
        // BUG-03 FIX: do not blindly overwrite an explicit CheckOut with a biometric one.
        var biometricLogs = newLogs.Where(x => x.Type == RawLogType.Biometric)
                                   .Select(x => x.Timestamp)
                                   .OrderBy(x => x)
                                   .ToList();
        if (biometricLogs.Any())
        {
          if (!checkIn.HasValue) checkIn = biometricLogs.First();

          var bioLast = biometricLogs.Last();
          checkOut = checkOut.HasValue
              ? (bioLast > checkOut.Value ? bioLast : checkOut)  // keep whichever is later
              : bioLast;
        }

        // --- BUG-01 FIX: Overnight cross-day checkout rerouting ---
        // If we have ONLY a CheckOut for the current logical day (no CheckIn found anywhere)
        // and the previous logical day has an open overnight ghost, attach this checkout there.
        if (!checkIn.HasValue && checkOut.HasValue)
        {
          var prevLogicalDate = workDate.AddDays(-1);
          var prevBucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(
              employeeId, prevLogicalDate.ToString("MM-yyyy"));
          var prevDayLog = prevBucket?.DailyLogs.FirstOrDefault(
              x => x.Date.Date == prevLogicalDate.Date);

          if (prevDayLog?.CheckIn.HasValue == true && !prevDayLog.CheckOut.HasValue)
          {
            var prevShift = await GetEffectiveShiftAsync(employeeId, prevLogicalDate);
            if (prevShift?.IsOvernight == true)
            {
              _logger.LogInformation(
                  "ProcessSingleGroupAsync: Overnight checkout rerouted → LogicalDate={PrevDate} EmployeeId={Id}",
                  prevLogicalDate.ToString("yyyy-MM-dd"), employeeId);

              prevDayLog.UpdateCheckTimes(prevDayLog.CheckIn, checkOut, prevShift.Code);
              _calculator.CalculateDailyStatus(prevDayLog, prevShift);
              prevBucket!.AddOrUpdateDailyLog(prevDayLog);
              await _attendanceRepo.UpdateAsync(prevBucket.Id, prevBucket);

              foreach (var log in newLogs) log.MarkAsProcessed();
              await _rawRepo.MarkManyAsProcessedAsync(newLogs.Select(l => l.Id));
              await _unitOfWork.CommitTransactionAsync();
              return;
            }
          }
        }

        // --- Defensive recovery: attempt to recover a missing CheckIn from history ---
        if (!checkIn.HasValue)
        {
          // Convert the logical-day window to a UTC range for the raw-log query
          var logicalStart = DateTime.SpecifyKind(
              workDate.Add(TimeSpan.FromHours(LogicalDayCutoffHour)), DateTimeKind.Unspecified);
          var logicalEnd   = DateTime.SpecifyKind(
              workDate.AddDays(1).Add(TimeSpan.FromHours(LogicalDayCutoffHour)), DateTimeKind.Unspecified);
          var dayStartUtc  = TimeZoneInfo.ConvertTimeToUtc(logicalStart, _timeZone);
          var dayEndUtc    = TimeZoneInfo.ConvertTimeToUtc(logicalEnd,   _timeZone);

          var allDayLogs = await _rawRepo.GetByDateRangeAsync(employeeId, dayStartUtc, dayEndUtc);
          var recoveredCheckIn = allDayLogs
              .Where(x => x.Type == RawLogType.CheckIn)
              .Select(x => (DateTime?)x.Timestamp)
              .DefaultIfEmpty(null)
              .Min();

          if (recoveredCheckIn.HasValue)
          {
            checkIn = recoveredCheckIn;
            _logger.LogWarning(
                "ProcessSingleGroupAsync: Recovered CheckIn={CheckIn} EmployeeId={Id} Date={Date}",
                checkIn, employeeId, workDate.ToString("yyyy-MM-dd"));
          }
        }

        dailyLog.UpdateCheckTimes(checkIn, checkOut, shift?.Code ?? "Unknown");

        // --- STEP D: Calculate ---
        _calculator.CalculateDailyStatus(dailyLog, shift);

        // --- STEP D0: Suppress OT if admin has not pre-approved this date ---
        // By default, working beyond shift end does NOT count as overtime.
        // Admin must create an OvertimeSchedule entry for the employee + date
        // before OT hours are recognised.
        if (dailyLog.OvertimeHours > 0)
        {
          bool otApproved = await _otScheduleRepo.ExistsAsync(employeeId, workDate);
          if (!otApproved)
          {
            _logger.LogDebug(
                "OT suppressed (no schedule): EmployeeId={Id} Date={Date} OT={OT}h",
                employeeId, workDate.ToString("yyyy-MM-dd"), dailyLog.OvertimeHours);

            dailyLog.UpdateCalculationResults(
                dailyLog.WorkingHours,
                dailyLog.LateMinutes,
                dailyLog.EarlyLeaveMinutes,
                overtimeHours: 0,
                dailyLog.Status,
                note: dailyLog.Note,
                isLate: dailyLog.IsLate,
                isEarlyLeave: dailyLog.IsEarlyLeave);
          }
        }

        // --- STEP D1: Flag missing check-in ---
        // Employee had a checkout but no check-in was found (not overnight, not recovered).
        // Mark the day so the employee can submit an explanation.
        if (!dailyLog.CheckIn.HasValue && dailyLog.CheckOut.HasValue)
        {
          dailyLog.UpdateCalculationResults(
              dailyLog.WorkingHours, dailyLog.LateMinutes, dailyLog.EarlyLeaveMinutes,
              dailyLog.OvertimeHours, dailyLog.Status,
              note: "[Missing] Quên check-in — vui lòng gửi giải trình",
              isLate: dailyLog.IsLate,
              isEarlyLeave: dailyLog.IsEarlyLeave,
              isMissingCheckIn: true);
        }

        // --- STEP D2: Apply holiday flag ---
        // Must run AFTER the calculator so OT/working-hours are not erased.
        if (holidayMap != null && holidayMap.TryGetValue(workDate.Date, out var holidayName))
        {
          dailyLog.SetHoliday(true, holidayName);
          _logger.LogInformation(
              "Holiday flag applied: EmployeeId={Id} Date={Date} Holiday={Name}",
              employeeId, workDate.ToString("yyyy-MM-dd"), holidayName);
        }

        // --- STEP E: Persist ---
        bucket.AddOrUpdateDailyLog(dailyLog);
        await _attendanceRepo.UpdateAsync(bucket.Id, bucket);

        foreach (var log in newLogs) log.MarkAsProcessed();
        await _rawRepo.MarkManyAsProcessedAsync(newLogs.Select(l => l.Id));

        await _unitOfWork.CommitTransactionAsync();
      }
      catch (Exception)
      {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
      }
    }

    // -------------------------------------------------------------------------
    // Ghost Log: auto-close a previous day that has CheckIn but no CheckOut
    // -------------------------------------------------------------------------
    private async Task ProcessGhostLogAsync(string employeeId, DateTime prevDate)
    {
      var prevMonthKey = prevDate.ToString("MM-yyyy");
      var bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(employeeId, prevMonthKey);
      if (bucket == null) return;

      var prevLog = bucket.DailyLogs.FirstOrDefault(x => x.Date.Date == prevDate.Date);
      if (prevLog == null || !prevLog.CheckIn.HasValue || prevLog.CheckOut.HasValue) return;

      var prevShift = await GetEffectiveShiftAsync(employeeId, prevDate);
      if (prevShift == null) return;

      // Auto-checkout = ShiftEnd of the ghost day, stored as UTC
      var shiftEndLocal = prevDate.Add(prevShift.EndTime);
      if (prevShift.IsOvernight) shiftEndLocal = shiftEndLocal.AddDays(1);
      var autoCheckOutUtc = TimeZoneInfo.ConvertTimeToUtc(
          DateTime.SpecifyKind(shiftEndLocal, DateTimeKind.Unspecified), _timeZone);

      prevLog.UpdateCheckTimes(prevLog.CheckIn, autoCheckOutUtc, prevShift.Code);

      // Run the calculator first…
      _calculator.CalculateDailyStatus(prevLog, prevShift);

      // BUG-02 FIX: append the system note AFTER the calculator, so it cannot be
      // overwritten by the UpdateCalculationResults call inside CalculateDailyStatus.
      prevLog.UpdateCalculationResults(
          prevLog.WorkingHours,
          prevLog.LateMinutes,
          prevLog.EarlyLeaveMinutes,
          prevLog.OvertimeHours,
          prevLog.Status,
          note: string.IsNullOrEmpty(prevLog.Note)
              ? "[Auto-closed] Missing checkout"
              : $"[Auto-closed] {prevLog.Note}",
          isLate: prevLog.IsLate,
          isEarlyLeave: prevLog.IsEarlyLeave,
          isMissingPunch: true);   // flag that this day was auto-closed

      bucket.AddOrUpdateDailyLog(prevLog);
      await _attendanceRepo.UpdateAsync(bucket.Id, bucket);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Maps a UTC punch timestamp to the logical work-date (local time, day-breaker applied).
    /// Punches before <see cref="LogicalDayCutoffHour"/> (06:00 AM) are assigned to the
    /// previous calendar day, so overnight shifts stay in one logical work-day.
    /// </summary>
    private DateTime GetLogicalDate(DateTime utcTimestamp)
    {
      var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, _timeZone);
      return localTime.TimeOfDay < TimeSpan.FromHours(LogicalDayCutoffHour)
          ? localTime.Date.AddDays(-1)
          : localTime.Date;
    }

    private async Task<Shift?> GetEffectiveShiftAsync(string employeeId, DateTime date)
    {
      var rosterShift = await _shiftRepo.GetShiftByDateAsync(employeeId, date);
      if (rosterShift != null) return rosterShift;

      var employee = await _employeeRepo.GetByIdAsync(employeeId);
      if (!string.IsNullOrEmpty(employee?.JobDetails.ShiftId))
        return await _shiftRepo.GetByIdAsync(employee.JobDetails.ShiftId);

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
      return bucket ?? new AttendanceBucket(employeeId, monthKey);
    }

    private async Task MarkGroupAsError(IEnumerable<RawAttendanceLog> group, string error)
    {
      foreach (var log in group)
      {
        log.MarkAsFailed(error);
        await _rawRepo.MarkAsErrorAsync(log.Id, error);
      }
    }

    // -------------------------------------------------------------------------
    // Backfill: retroactively mark holiday flags for a whole month
    // -------------------------------------------------------------------------
    public async Task<int> BackfillHolidayFlagsAsync(int month, int year)
    {
      var monthKey = $"{month:D2}-{year}";
      var monthStart = new DateTime(year, month, 1);
      var monthEnd = monthStart.AddMonths(1).AddDays(-1);

      // Load all attendance buckets for the month
      var buckets = (await _attendanceRepo.GetByMonthAsync(monthKey)).ToList();
      if (!buckets.Any())
      {
        _logger.LogInformation("BackfillHolidayFlagsAsync: No buckets found for {MonthKey}", monthKey);
        return 0;
      }

      // Load holidays for the month (one round-trip)
      var holidays = await _holidayRepo.GetByDateRangeAsync(monthStart, monthEnd);
      var holidayMap = holidays.ToDictionary(h => h.Date.Date, h => h.Name);

      if (!holidayMap.Any())
      {
        _logger.LogInformation("BackfillHolidayFlagsAsync: No holidays found for {MonthKey}", monthKey);
        return 0;
      }

      int updatedCount = 0;

      foreach (var bucket in buckets)
      {
        bool bucketChanged = false;

        foreach (var dailyLog in bucket.DailyLogs)
        {
          if (holidayMap.TryGetValue(dailyLog.Date.Date, out var holidayName) && !dailyLog.IsHoliday)
          {
            dailyLog.SetHoliday(true, holidayName);
            bucketChanged = true;
            _logger.LogInformation(
                "BackfillHolidayFlagsAsync: Marked holiday for EmployeeId={Id} Date={Date} Holiday={Name}",
                bucket.EmployeeId, dailyLog.Date.ToString("yyyy-MM-dd"), holidayName);
          }
        }

        if (bucketChanged)
        {
          bucket.RecalculateTotals();
          await _attendanceRepo.UpdateAsync(bucket.Id, bucket);
          updatedCount++;
        }
      }

      _logger.LogInformation(
          "BackfillHolidayFlagsAsync: {Count} buckets updated for {MonthKey}", updatedCount, monthKey);
      return updatedCount;
    }
  }
}