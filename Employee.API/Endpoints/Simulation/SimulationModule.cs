using Carter;
using Employee.API.Common;
using Employee.Domain.Entities.Simulation;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Employee.API.Endpoints.Simulation;

/// <summary>
/// Admin-only endpoints to manage simulation bots and monitor simulation activity.
/// These endpoints are available in all environments (the Dev module is dev-only).
/// </summary>
public class SimulationModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/simulation")
                       .WithTags("Simulation Engine")
                       .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

        // ── Dashboard ─────────────────────────────────────────────────────────
        group.MapGet("/dashboard", SimulationHandlers.GetDashboard)
             .WithName("GetSimulationDashboard")
             .WithDescription("Returns simulation engine stats: active bots, success rate, today's activity.");

        // ── Bots ────────────────────────────────────────────────────────────
        group.MapGet("/bots", SimulationHandlers.GetBots)
             .WithName("GetSimulationBots")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapGet("/bots/{id}", SimulationHandlers.GetBotById)
             .WithName("GetSimulationBotById")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapPost("/bots", SimulationHandlers.CreateBot)
             .WithName("CreateSimulationBot")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapPatch("/bots/{id}/pause", SimulationHandlers.PauseBot)
             .WithName("PauseSimulationBot")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapPatch("/bots/{id}/resume", SimulationHandlers.ResumeBot)
             .WithName("ResumeSimulationBot")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapDelete("/bots/{id}", SimulationHandlers.DeleteBot)
             .WithName("DeleteSimulationBot")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        // ── Provisioning ────────────────────────────────────────────────────
        group.MapPost("/provision", SimulationHandlers.ProvisionBots)
             .WithName("ProvisionSimulationBots")
             .WithDescription("Creates simulation bot accounts for all active employees that don't have one yet. Idempotent.")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        // ── Manual Trigger ───────────────────────────────────────────────────
        group.MapPost("/run", SimulationHandlers.RunDailySimulation)
             .WithName("RunDailySimulation")
             .WithDescription("Manually triggers the daily simulation run for all active bots.")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapPost("/bots/{id}/simulate", SimulationHandlers.SimulateSingleBot)
             .WithName("SimulateSingleBot")
             .WithDescription("Simulates one bot for today (or a specific date via ?date=yyyy-MM-dd).")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        // ── Logs ─────────────────────────────────────────────────────────────
        group.MapGet("/logs", SimulationHandlers.GetLogs)
             .WithName("GetSimulationLogs")
             .RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapGet("/bots/{botId}/logs", SimulationHandlers.GetBotLogs)
             .WithName("GetBotSimulationLogs")
             .RequireAuthorization(p => p.RequireRole("Admin"));
    }
}

public static class SimulationHandlers
{
    /// <summary>
    /// GET /api/simulation/dashboard
    /// </summary>
    public static async Task<IResult> GetDashboard(ISimulationService service)
    {
        var stats = await service.GetDashboardStatsAsync();
        return ResultUtils.Success(stats, "Simulation dashboard data retrieved.");
    }

    /// <summary>
    /// GET /api/simulation/bots
    /// </summary>
    public static async Task<IResult> GetBots(
        [FromServices] ISimulationBotRepository botRepo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SimulationBotStatus? status = null)
    {
        var filter = status.HasValue
            ? new { Status = status.Value }
            : null;

        var paged = await botRepo.GetPagedAsync(
            new Employee.Domain.Common.Models.PaginationParams { PageNumber = page, PageSize = pageSize });

        var items = paged.Items
            .Select(b => new
            {
                b.Id,
                b.EmployeeId,
                b.EmployeeCode,
                b.EmployeeName,
                b.Status,
                b.ActivityProfile.ProfileName,
                b.LastSimulatedAt,
                b.TotalSimulationsRun,
                b.ConsecutiveFailures,
                b.ShiftHours,
                WorkDays = b.WorkDays.Select(d => d.ToString()).ToList()
            })
            .ToList();

        return ResultUtils.Success(new
        {
            items,
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize
        });
    }

    /// <summary>
    /// GET /api/simulation/bots/{id}
    /// </summary>
    public static async Task<IResult> GetBotById(
        string id,
        [FromServices] ISimulationBotRepository botRepo)
    {
        var bot = await botRepo.GetByIdAsync(id);
        if (bot == null)
            return ResultUtils.Fail("BOT_NOT_FOUND", "Simulation bot not found.", 404);

        return ResultUtils.Success(new
        {
            bot.Id,
            bot.EmployeeId,
            bot.EmployeeCode,
            bot.EmployeeName,
            bot.Status,
            bot.ActivityProfile,
            bot.LastSimulatedAt,
            bot.TotalSimulationsRun,
            bot.ConsecutiveFailures,
            bot.CheckInWindowStartUtc,
            bot.CheckInWindowEndUtc,
            bot.ShiftHours,
            WorkDays = bot.WorkDays.Select(d => d.ToString()).ToList(),
            bot.CreatedAt
        });
    }

    /// <summary>
    /// POST /api/simulation/bots
    /// Creates a single simulation bot for a specific employee.
    /// </summary>
    public static async Task<IResult> CreateBot(
        [FromBody] CreateSimulationBotRequest request,
        [FromServices] ISimulationBotRepository botRepo,
        [FromServices] IEmployeeRepository empRepo)
    {
        var emp = await empRepo.GetByIdAsync(request.EmployeeId);
        if (emp == null)
            return ResultUtils.Fail("EMPLOYEE_NOT_FOUND", "Employee not found.", 404);

        var existing = await botRepo.GetByEmployeeIdAsync(request.EmployeeId);
        if (existing != null)
            return ResultUtils.Fail("BOT_EXISTS", "This employee already has a simulation bot.", 409);

        var bot = new SimulationBot(emp.Id, emp.EmployeeCode, emp.FullName)
        {
            ActivityProfile = request.Profile ?? new SimulationActivityProfile { ProfileName = "Regular" },
            Status = SimulationBotStatus.Active,
            ShiftHours = request.ShiftHours ?? 8.0
        };

        await botRepo.CreateAsync(bot);
        return ResultUtils.Created(bot.Id, $"Simulation bot created for {emp.FullName}.");
    }

    /// <summary>
    /// PATCH /api/simulation/bots/{id}/pause
    /// </summary>
    public static async Task<IResult> PauseBot(
        string id,
        [FromServices] ISimulationBotRepository botRepo)
    {
        var bot = await botRepo.GetByIdAsync(id);
        if (bot == null)
            return ResultUtils.Fail("BOT_NOT_FOUND", "Simulation bot not found.", 404);

        bot.Status = SimulationBotStatus.Paused;
        bot.UpdatedAt = DateTime.UtcNow;
        await botRepo.UpdateAsync(bot.Id, bot);

        return ResultUtils.Success($"Bot '{bot.EmployeeName}' has been paused.");
    }

    /// <summary>
    /// PATCH /api/simulation/bots/{id}/resume
    /// </summary>
    public static async Task<IResult> ResumeBot(
        string id,
        [FromServices] ISimulationBotRepository botRepo)
    {
        var bot = await botRepo.GetByIdAsync(id);
        if (bot == null)
            return ResultUtils.Fail("BOT_NOT_FOUND", "Simulation bot not found.", 404);

        bot.Status = SimulationBotStatus.Active;
        bot.ConsecutiveFailures = 0;
        bot.UpdatedAt = DateTime.UtcNow;
        await botRepo.UpdateAsync(bot.Id, bot);

        return ResultUtils.Success($"Bot '{bot.EmployeeName}' has been resumed.");
    }

    /// <summary>
    /// DELETE /api/simulation/bots/{id}
    /// </summary>
    public static async Task<IResult> DeleteBot(
        string id,
        [FromServices] ISimulationBotRepository botRepo)
    {
        await botRepo.DeleteAsync(id);
        return ResultUtils.Success("Simulation bot has been removed.");
    }

    /// <summary>
    /// POST /api/simulation/provision
    /// Creates bot accounts for all active employees.
    /// </summary>
    public static async Task<IResult> ProvisionBots(ISimulationService service)
    {
        var result = await service.ProvisionBotsAsync();
        return ResultUtils.Success(result,
            $"Provisioning complete: {result.BotsCreated} bots created, {result.BotsSkipped} already existed.");
    }

    /// <summary>
    /// POST /api/simulation/run
    /// Manually triggers both morning and evening simulations for all active bots.
    /// </summary>
    public static async Task<IResult> RunDailySimulation(ISimulationService service)
    {
        var morningResult = await service.RunMorningSimulationAsync();
        var eveningResult = await service.RunEveningSimulationAsync();
        return ResultUtils.Success(new { morningResult, eveningResult },
            $"Simulation run completed. Morning ok: {morningResult.SuccessCount}. Evening ok: {eveningResult.SuccessCount}.");
    }

    /// <summary>
    /// POST /api/simulation/bots/{id}/simulate
    /// </summary>
    public static async Task<IResult> SimulateSingleBot(
        string botId,
        [FromQuery] string? date,
        ISimulationService service)
    {
        var simDate = string.IsNullOrWhiteSpace(date)
            ? DateTime.UtcNow.AddHours(7).Date
            : DateTime.ParseExact(date, "yyyy-MM-dd", null);

        var morning = await service.SimulateMorningBotAsync(botId, simDate);
        var evening = await service.SimulateEveningBotAsync(botId, simDate);
        return ResultUtils.Success(new { morning, evening }, $"Simulation for bot completed.");
    }

    /// <summary>
    /// GET /api/simulation/logs
    /// </summary>
    public static async Task<IResult> GetLogs(
        [FromServices] ISimulationLogRepository logRepo,
        [FromQuery] int days = 7,
        [FromQuery] int limit = 100)
    {
        var from = DateTime.UtcNow.Date.AddDays(-Math.Min(days, 30));
        var logs = await logRepo.GetByDateRangeAsync(from, DateTime.UtcNow.AddDays(1));
        var limited = logs.Take(limit).ToList();

        return ResultUtils.Success(new
        {
            TotalCount = logs.Count,
            ReturnedCount = limited.Count,
            Logs = limited.Select(l => new
            {
                l.Id,
                l.BotId,
                l.EmployeeId,
                l.EmployeeName,
                l.SimulatedDateUtc,
                l.SimulatedAtUtc,
                ActionType = l.ActionType.ToString(),
                l.ActionDescription,
                Result = l.Result.ToString(),
                l.ErrorMessage,
                l.TargetEntityId,
                l.HttpStatusCode,
                l.DurationMs
            })
        });
    }

    /// <summary>
    /// GET /api/simulation/bots/{botId}/logs
    /// </summary>
    public static async Task<IResult> GetBotLogs(
        string botId,
        [FromServices] ISimulationLogRepository logRepo,
        [FromQuery] int limit = 50)
    {
        var logs = await logRepo.GetByBotIdAsync(botId, limit);
        return ResultUtils.Success(new
        {
            BotId = botId,
            Count = logs.Count,
            Logs = logs.Select(l => new
            {
                l.Id,
                l.SimulatedDateUtc,
                l.SimulatedAtUtc,
                ActionType = l.ActionType.ToString(),
                l.ActionDescription,
                Result = l.Result.ToString(),
                l.ErrorMessage,
                l.HttpStatusCode,
                l.DurationMs
            })
        });
    }
}

public class CreateSimulationBotRequest
{
    public string EmployeeId { get; set; } = string.Empty;
    public SimulationActivityProfile? Profile { get; set; }
    public double? ShiftHours { get; set; }
}
