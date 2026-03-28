using System.Diagnostics;
using Employee.Domain.Entities.Simulation;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Enums;
using Employee.Application.Features.Attendance.Commands.CheckIn;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Features.Leave.Commands.CreateLeaveRequest;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Employee.Infrastructure.Services;

/// <summary>
/// Orchestrates daily simulation runs for all active bots.
/// Executes realistic employee workflows: check-in, check-out, leave requests
/// through the same MediatR pipeline as real users.
/// </summary>
public interface ISimulationService
{
    Task<DailySimulationResult> RunDailySimulationAsync(CancellationToken ct = default);
    Task<BotProvisioningResult> ProvisionBotsAsync(CancellationToken ct = default);
    Task<BotSimulationResult> SimulateBotAsync(string botId, DateTime simulationDate, CancellationToken ct = default);
    Task<SimulationDashboardStats> GetDashboardStatsAsync(CancellationToken ct = default);
}

public class SimulationService : ISimulationService
{
    private readonly ISimulationBotRepository _botRepo;
    private readonly ISimulationLogRepository _logRepo;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly ILeaveAllocationRepository _leaveAllocRepo;
    private readonly IMediator _mediator;
    private readonly ILogger<SimulationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly Random _rng = new();
    private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

    public SimulationService(
        ISimulationBotRepository botRepo,
        ISimulationLogRepository logRepo,
        ILeaveTypeRepository leaveTypeRepo,
        ILeaveAllocationRepository leaveAllocRepo,
        IMediator mediator,
        ILogger<SimulationService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _botRepo = botRepo;
        _logRepo = logRepo;
        _leaveTypeRepo = leaveTypeRepo;
        _leaveAllocRepo = leaveAllocRepo;
        _mediator = mediator;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<DailySimulationResult> RunDailySimulationAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var todayVn = DateTime.UtcNow.Add(VnOffset).Date;
        var bots = await _botRepo.GetActiveBotsAsync(ct);

        if (bots.Count == 0)
        {
            _logger.LogInformation("No active simulation bots found. Skipping daily run.");
            return new DailySimulationResult
            {
                RunAt = DateTime.UtcNow,
                SimulationDate = todayVn,
                TotalBots = 0,
                SuccessCount = 0,
                FailureCount = 0,
                SkippedCount = 0
            };
        }

        _logger.LogInformation(
            "Daily simulation starting: {Count} active bots for {Date:yyyy-MM-dd}",
            bots.Count, todayVn);

        int successCount = 0, failureCount = 0, skippedCount = 0;
        var results = new List<BotSimulationResult>();

        foreach (var bot in bots)
        {
            var result = await SimulateBotAsync(bot.Id, todayVn, ct);

            switch (result.OverallResult)
            {
                case SimulationOverallResult.Success: successCount++; break;
                case SimulationOverallResult.Failed:   failureCount++; break;
                case SimulationOverallResult.Skipped:  skippedCount++; break;
            }
            results.Add(result);
        }

        sw.Stop();
        _logger.LogInformation(
            "Daily simulation completed in {ElapsedMs}ms: {Success} ok, {Failed} failed, {Skipped} skipped",
            sw.ElapsedMilliseconds, successCount, failureCount, skippedCount);

        return new DailySimulationResult
        {
            RunAt = DateTime.UtcNow,
            SimulationDate = todayVn,
            TotalBots = bots.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            SkippedCount = skippedCount,
            DurationMs = sw.ElapsedMilliseconds,
            BotResults = results
        };
    }

    public async Task<BotSimulationResult> SimulateBotAsync(
        string botId, DateTime simulationDate, CancellationToken ct = default)
    {
        var bot = await _botRepo.GetByIdAsync(botId, ct);
        if (bot == null)
            return new BotSimulationResult { BotId = botId, SimulationDate = simulationDate, OverallResult = SimulationOverallResult.Failed, ErrorMessage = "Bot not found" };

        var todayVn = simulationDate.Date;
        var todayUtc = todayVn.Subtract(VnOffset);

        if (!bot.IsWorkDay(todayUtc))
        {
            _logger.LogDebug("Bot {Name} skipped {Date} (not a work day)", bot.EmployeeName, todayVn);
            return new BotSimulationResult
            {
                BotId = botId, EmployeeId = bot.EmployeeId, EmployeeName = bot.EmployeeName,
                SimulationDate = simulationDate, OverallResult = SimulationOverallResult.Skipped, SkippedReason = "Non-working day"
            };
        }

        var sw = Stopwatch.StartNew();
        var actions = new List<SimulationActionResult>();

        try
        {
            var checkInResult = await SimulateCheckInAsync(bot, todayUtc, ct);
            actions.Add(checkInResult);

            if (checkInResult.ActionResult == DomainSimulationActionResult.Success)
            {
                var checkOutResult = await SimulateCheckOutAsync(bot, todayUtc, ct);
                actions.Add(checkOutResult);
            }

            var leaveResult = await SimulateLeaveRequestAsync(bot, todayVn, ct);
            if (leaveResult != null) actions.Add(leaveResult);

            sw.Stop();
            bot.MarkSimulationRun(DateTime.UtcNow);
            await _botRepo.UpdateAsync(bot.Id, bot, ct);

            return new BotSimulationResult
            {
                BotId = botId, EmployeeId = bot.EmployeeId, EmployeeName = bot.EmployeeName,
                SimulationDate = simulationDate, Actions = actions,
                OverallResult = SimulationOverallResult.Success, DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Simulation failed for bot {Name} ({Id})", bot.EmployeeName, botId);
            bot.MarkSimulationFailure();
            await _botRepo.UpdateAsync(bot.Id, bot, ct);
            return new BotSimulationResult
            {
                BotId = botId, EmployeeId = bot.EmployeeId, EmployeeName = bot.EmployeeName,
                SimulationDate = simulationDate, Actions = actions,
                OverallResult = SimulationOverallResult.Failed, ErrorMessage = ex.Message, DurationMs = sw.ElapsedMilliseconds
            };
        }
    }

    private async Task<SimulationActionResult> SimulateCheckInAsync(SimulationBot bot, DateTime simulationDateUtc, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (_rng.NextDouble() > bot.ActivityProfile.AttendanceProbability)
            {
                var log = SimulationLog.Create(bot.Id, bot.EmployeeId, bot.EmployeeName,
                    SimulationActionType.CheckIn, "Absent today (attendance probability)",
                    SimulationLogResult.Skipped, DateTime.UtcNow);
                await _logRepo.CreateAsync(log, ct);
                return new SimulationActionResult { ActionType = SimulationActionType.CheckIn, Description = log.ActionDescription, ActionResult = DomainSimulationActionResult.Skipped };
            }

            var onTime = _rng.NextDouble() < bot.ActivityProfile.OnTimeProbability;
            var varianceMinutes = _rng.Next(-bot.ActivityProfile.CheckInVarianceMinutes, bot.ActivityProfile.CheckInVarianceMinutes + 1);
            var checkInUtc = simulationDateUtc.Date.Add(bot.CheckInWindowStartUtc).AddMinutes(varianceMinutes);
            if (!onTime) checkInUtc = checkInUtc.AddMinutes(_rng.Next(5, 31));

            await _mediator.Send(new CheckInCommand
            {
                Dto = new CheckInRequestDto { Type = "CheckIn", EmployeeId = bot.EmployeeId, DeviceId = $"SimBot-{bot.EmployeeCode}" },
                EmployeeId = bot.EmployeeId
            }, ct);

            sw.Stop();
            var localTime = checkInUtc.Add(VnOffset);
            var desc = $"Checked in at {localTime:HH:mm} ICT" + (onTime ? "" : " (late)");
            var log2 = SimulationLog.Create(bot.Id, bot.EmployeeId, bot.EmployeeName, SimulationActionType.CheckIn, desc, SimulationLogResult.Success, DateTime.UtcNow);
            log2.MarkSuccess(null, 200, sw.ElapsedMilliseconds);
            await _logRepo.CreateAsync(log2, ct);

            return new SimulationActionResult { ActionType = SimulationActionType.CheckIn, Description = desc, ActionResult = DomainSimulationActionResult.Success, HttpStatusCode = 200 };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var log = SimulationLog.Create(bot.Id, bot.EmployeeId, bot.EmployeeName, SimulationActionType.CheckIn, "Check-in failed", SimulationLogResult.Failed, DateTime.UtcNow);
            log.MarkFailure(ex.Message, 500, sw.ElapsedMilliseconds);
            await _logRepo.CreateAsync(log, ct);
            return new SimulationActionResult { ActionType = SimulationActionType.CheckIn, Description = "Check-in failed", ActionResult = DomainSimulationActionResult.Failed, ErrorMessage = ex.Message };
        }
    }

    private async Task<SimulationActionResult> SimulateCheckOutAsync(SimulationBot bot, DateTime simulationDateUtc, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var checkInUtc = simulationDateUtc.Date.Add(bot.CheckInWindowStartUtc);
            var checkOutUtc = checkInUtc.AddHours(bot.ShiftHours).AddMinutes(_rng.Next(0, 31));

            await _mediator.Send(new CheckInCommand
            {
                Dto = new CheckInRequestDto { Type = "CheckOut", EmployeeId = bot.EmployeeId, DeviceId = $"SimBot-{bot.EmployeeCode}" },
                EmployeeId = bot.EmployeeId
            }, ct);

            sw.Stop();
            var localTime = checkOutUtc.Add(VnOffset);
            var ot = _rng.Next(0, 31);
            var overtime = ot > 5 ? $" (+{ot}min OT)" : "";
            var desc = $"Checked out at {localTime:HH:mm} ICT{overtime}";
            var log = SimulationLog.Create(bot.Id, bot.EmployeeId, bot.EmployeeName, SimulationActionType.CheckOut, desc, SimulationLogResult.Success, DateTime.UtcNow);
            log.MarkSuccess(null, 200, sw.ElapsedMilliseconds);
            await _logRepo.CreateAsync(log, ct);

            return new SimulationActionResult { ActionType = SimulationActionType.CheckOut, Description = desc, ActionResult = DomainSimulationActionResult.Success, HttpStatusCode = 200 };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var log = SimulationLog.Create(bot.Id, bot.EmployeeId, bot.EmployeeName, SimulationActionType.CheckOut, "Check-out failed", SimulationLogResult.Failed, DateTime.UtcNow);
            log.MarkFailure(ex.Message, 500, sw.ElapsedMilliseconds);
            await _logRepo.CreateAsync(log, ct);
            return new SimulationActionResult { ActionType = SimulationActionType.CheckOut, Description = "Check-out failed", ActionResult = DomainSimulationActionResult.Failed, ErrorMessage = ex.Message };
        }
    }

    private async Task<SimulationActionResult?> SimulateLeaveRequestAsync(SimulationBot bot, DateTime simulationDateVn, CancellationToken ct)
    {
        if (simulationDateVn.Day > 5) return null;
        if (_rng.NextDouble() > bot.ActivityProfile.MonthlyLeaveProbability) return null;

        var sw = Stopwatch.StartNew();
        try
        {
            var leaveTypes = (await _leaveTypeRepo.GetAllAsync(ct)).Where(l => !l.IsDeleted && l.IsActive).ToList();
            if (leaveTypes.Count == 0) { _logger.LogWarning("No active leave types for bot {Name}", bot.EmployeeName); return null; }

            var chosenType = leaveTypes[_rng.Next(leaveTypes.Count)];
            var duration = _rng.Next(1, 4);
            var fromDate = simulationDateVn.AddDays(_rng.Next(1, 10));
            var toDate = fromDate.AddDays(duration - 1);

            var allocation = await _leaveAllocRepo.GetByEmployeeAndTypeAsync(bot.EmployeeId, chosenType.Id, fromDate.Year.ToString(), ct);
            var remaining = allocation?.CurrentBalance ?? 0;
            if (remaining < duration)
            {
                var desc = $"Leave request skipped: insufficient balance (need {duration}d, have {remaining}d of {chosenType.Name})";
                var log = SimulationLog.Create(bot.Id, bot.EmployeeId, bot.EmployeeName, SimulationActionType.LeaveRequest, desc, SimulationLogResult.Skipped, DateTime.UtcNow);
                await _logRepo.CreateAsync(log, ct);
                return new SimulationActionResult { ActionType = SimulationActionType.LeaveRequest, Description = desc, ActionResult = DomainSimulationActionResult.Skipped };
            }

            var reasons = new[] { "Personal appointment / family matter", "Health check-up", "Home maintenance", "Personal travel", "Rest and recovery" };
            var result = await _mediator.Send(new CreateLeaveRequestCommand
            {
                EmployeeId = bot.EmployeeId, LeaveType = chosenType.Code,
                FromDate = fromDate, ToDate = toDate, Reason = reasons[_rng.Next(reasons.Length)]
            }, ct);

            sw.Stop();
            var desc2 = $"Leave request submitted: {chosenType.Name}, {fromDate:MMM dd}–{toDate:MMM dd} ({duration}d)";
            var log2 = SimulationLog.Create(bot.Id, bot.EmployeeId, bot.EmployeeName, SimulationActionType.LeaveRequest, desc2, SimulationLogResult.Success, DateTime.UtcNow);
            log2.MarkSuccess(result.Id, 201, sw.ElapsedMilliseconds);
            await _logRepo.CreateAsync(log2, ct);
            return new SimulationActionResult { ActionType = SimulationActionType.LeaveRequest, Description = desc2, ActionResult = DomainSimulationActionResult.Success, HttpStatusCode = 201, TargetEntityId = result.Id };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var log = SimulationLog.Create(bot.Id, bot.EmployeeId, bot.EmployeeName, SimulationActionType.LeaveRequest, "Leave request failed", SimulationLogResult.Failed, DateTime.UtcNow);
            log.MarkFailure(ex.Message, 500, sw.ElapsedMilliseconds);
            await _logRepo.CreateAsync(log, ct);
            return new SimulationActionResult { ActionType = SimulationActionType.LeaveRequest, Description = "Leave request failed", ActionResult = DomainSimulationActionResult.Failed, ErrorMessage = ex.Message };
        }
    }

    public async Task<BotProvisioningResult> ProvisionBotsAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var empRepo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var employees = await empRepo.GetAllActiveAsync(ct);
        int created = 0, skipped = 0;
        var profiles = GetPredefinedProfiles();

        foreach (var emp in employees)
        {
            if (await _botRepo.GetByEmployeeIdAsync(emp.Id, ct) != null) { skipped++; continue; }
            var profile = profiles[_rng.Next(profiles.Length)];
            var bot = new SimulationBot(emp.Id, emp.EmployeeCode, emp.FullName) { ActivityProfile = profile, Status = SimulationBotStatus.Active };
            await _botRepo.CreateAsync(bot, ct);
            created++;
        }

        _logger.LogInformation("Bot provisioning: {Created} created, {Skipped} already existed", created, skipped);
        return new BotProvisioningResult { TotalEmployees = employees.Count, BotsCreated = created, BotsSkipped = skipped, ProfilesUsed = profiles.Select(p => p.ProfileName).Distinct().ToList() };
    }

    public async Task<SimulationDashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var totalBots = await _botRepo.CountActiveAsync(ct);
        var totalLogs = await _logRepo.GetByDateRangeAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(1), ct);
        var actionStats = await _logRepo.GetActionStatsAsync(7, ct);
        var successRate = await _logRepo.GetSuccessRateAsync(7, ct);
        var todayLogs = await _logRepo.GetByDateAsync(DateTime.UtcNow, ct);

        return new SimulationDashboardStats
        {
            ActiveBots = totalBots,
            TotalSimulationsLast7Days = totalLogs.Count,
            SuccessRateLast7Days = Math.Round(successRate, 1),
            ActionBreakdown = actionStats,
            TodayTotalActions = todayLogs.Count,
            TodaySuccessCount = todayLogs.Count(l => l.Result == SimulationLogResult.Success),
            TodayFailedCount = todayLogs.Count(l => l.Result == SimulationLogResult.Failed),
            Last7DaysLogs = totalLogs.Take(50).ToList()
        };
    }

    private static SimulationActivityProfile[] GetPredefinedProfiles() => new[]
    {
        new SimulationActivityProfile { ProfileName = "Punctual Pro",    OnTimeProbability = 0.95, AttendanceProbability = 0.99, MonthlyLeaveProbability = 0.08, CheckInVarianceMinutes = 5,  MaxOvertimeHoursPerDay = 1.5, SaturdayWorkProbability = 0.05 },
        new SimulationActivityProfile { ProfileName = "Regular",         OnTimeProbability = 0.85, AttendanceProbability = 0.97, MonthlyLeaveProbability = 0.15, CheckInVarianceMinutes = 15, MaxOvertimeHoursPerDay = 2.0, SaturdayWorkProbability = 0.10 },
        new SimulationActivityProfile { ProfileName = "WFH Enthusiast",   OnTimeProbability = 0.70, AttendanceProbability = 0.99, MonthlyLeaveProbability = 0.20, CheckInVarianceMinutes = 30, MaxOvertimeHoursPerDay = 3.0, SaturdayWorkProbability = 0.30 },
        new SimulationActivityProfile { ProfileName = "Flexible Worker",  OnTimeProbability = 0.60, AttendanceProbability = 0.93, MonthlyLeaveProbability = 0.25, CheckInVarianceMinutes = 60, MaxOvertimeHoursPerDay = 1.0, SaturdayWorkProbability = 0.15 },
        new SimulationActivityProfile { ProfileName = "Weekend Warrior", OnTimeProbability = 0.90, AttendanceProbability = 0.95, MonthlyLeaveProbability = 0.12, CheckInVarianceMinutes = 10, MaxOvertimeHoursPerDay = 2.5, SaturdayWorkProbability = 0.50 },
        new SimulationActivityProfile { ProfileName = "Careful Planner",  OnTimeProbability = 0.92, AttendanceProbability = 0.98, MonthlyLeaveProbability = 0.18, CheckInVarianceMinutes = 8,  MaxOvertimeHoursPerDay = 1.0, SaturdayWorkProbability = 0.05 }
    };
}

// =============================================================================
// RESULT DTOs
// =============================================================================

public class DailySimulationResult
{
    public DateTime RunAt { get; set; }
    public DateTime SimulationDate { get; set; }
    public int TotalBots { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int SkippedCount { get; set; }
    public double DurationMs { get; set; }
    public List<BotSimulationResult> BotResults { get; set; } = new();
}

public class BotSimulationResult
{
    public string BotId { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public DateTime SimulationDate { get; set; }
    public SimulationOverallResult OverallResult { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SkippedReason { get; set; }
    public double DurationMs { get; set; }
    public List<SimulationActionResult> Actions { get; set; } = new();
}

public class SimulationActionResult
{
    public SimulationActionType ActionType { get; set; }
    public string Description { get; set; } = "";
    public DomainSimulationActionResult ActionResult { get; set; }
    public double DurationMs { get; set; }
    public int HttpStatusCode { get; set; }
    public string? TargetEntityId { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum DomainSimulationActionResult { Success, Failed, Skipped }

public enum SimulationOverallResult { Success, Failed, Skipped }

public class BotProvisioningResult
{
    public int TotalEmployees { get; set; }
    public int BotsCreated { get; set; }
    public int BotsSkipped { get; set; }
    public List<string> ProfilesUsed { get; set; } = new();
}

public class SimulationDashboardStats
{
    public long ActiveBots { get; set; }
    public int TotalSimulationsLast7Days { get; set; }
    public double SuccessRateLast7Days { get; set; }
    public Dictionary<SimulationActionType, int> ActionBreakdown { get; set; } = new();
    public int TodayTotalActions { get; set; }
    public int TodaySuccessCount { get; set; }
    public int TodayFailedCount { get; set; }
    public List<SimulationLog> Last7DaysLogs { get; set; } = new();
}
