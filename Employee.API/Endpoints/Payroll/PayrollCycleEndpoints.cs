using Carter;
using Employee.API.Common;
using Employee.Application.Features.Payroll.Services;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Payroll
{
  // ── Carter Module ──────────────────────────────────────────────────────────
  public class PayrollCycleModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/payroll-cycles")
                     .WithTags("Payroll Cycles")
                     .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // Tạo / lấy chu kỳ lương của một tháng (idempotent)
      group.MapPost("/generate", PayrollCycleHandlers.Generate);

      // Danh sách chu kỳ theo năm
      group.MapGet("/year/{year:int}", PayrollCycleHandlers.GetByYear);

      // Chi tiết một chu kỳ
      group.MapGet("/{monthKey}", PayrollCycleHandlers.GetByMonthKey);

      // Đóng chu kỳ (chốt sổ)
      group.MapPut("/{monthKey}/close", PayrollCycleHandlers.Close);

      // Hủy chu kỳ
      group.MapPut("/{monthKey}/cancel", PayrollCycleHandlers.Cancel);
    }
  }

  // ── Handlers ──────────────────────────────────────────────────────────────
  public static class PayrollCycleHandlers
  {
    public record GenerateCycleRequest(int Month, int Year);

    /// <summary>
    /// POST /api/payroll-cycles/generate
    /// Tạo chu kỳ lương nếu chưa tồn tại, hoặc trả về chu kỳ đã có (idempotent).
    /// Đây là bước BẮT BUỘC trước khi chạy /api/payrolls/generate.
    /// </summary>
    public static async Task<IResult> Generate(
        [FromBody] GenerateCycleRequest request,
        IPayrollCycleService cycleService)
    {
      if (request.Month < 1 || request.Month > 12)
        return ResultUtils.Fail("VALIDATION_ERROR", "Month phải trong khoảng 1–12.", 400);
      if (request.Year < 2000 || request.Year > 2100)
        return ResultUtils.Fail("VALIDATION_ERROR", "Year không hợp lệ.", 400);

      var cycle = await cycleService.GeneratePayrollCycleAsync(request.Month, request.Year);
      return ResultUtils.Success(new
      {
        cycle.Id,
        cycle.MonthKey,
        StartDate = cycle.StartDate.ToString("dd/MM/yyyy"),
        EndDate = cycle.EndDate.ToString("dd/MM/yyyy"),
        cycle.StandardWorkingDays,
        cycle.PublicHolidaysExcluded,
        cycle.WeeklyDaysOffSnapshot,
        Status = cycle.Status.ToString()
      }, $"Chu kỳ lương {cycle.MonthKey}: {cycle.StartDate:dd/MM/yyyy}–{cycle.EndDate:dd/MM/yyyy}, {cycle.StandardWorkingDays} ngày công.");
    }

    /// <summary>
    /// GET /api/payroll-cycles/year/{year}
    /// Lấy tất cả chu kỳ lương trong một năm.
    /// </summary>
    public static async Task<IResult> GetByYear(int year, IPayrollCycleService cycleService)
    {
      if (year < 2000 || year > 2100)
        return ResultUtils.Fail("VALIDATION_ERROR", "Year không hợp lệ.", 400);

      var cycles = await cycleService.GetCyclesByYearAsync(year);
      return ResultUtils.Success(cycles.Select(c => new
      {
        c.Id,
        c.MonthKey,
        StartDate = c.StartDate.ToString("dd/MM/yyyy"),
        EndDate = c.EndDate.ToString("dd/MM/yyyy"),
        c.StandardWorkingDays,
        c.PublicHolidaysExcluded,
        Status = c.Status.ToString()
      }));
    }

    /// <summary>
    /// GET /api/payroll-cycles/{monthKey}  (VD: 03-2026)
    /// </summary>
    public static async Task<IResult> GetByMonthKey(
        string monthKey, IPayrollCycleService cycleService)
    {
      try
      {
        var cycle = await cycleService.GetCycleAsync(monthKey);
        return ResultUtils.Success(new
        {
          cycle.Id,
          cycle.MonthKey,
          StartDate = cycle.StartDate.ToString("dd/MM/yyyy"),
          EndDate = cycle.EndDate.ToString("dd/MM/yyyy"),
          cycle.StandardWorkingDays,
          cycle.PublicHolidaysExcluded,
          cycle.WeeklyDaysOffSnapshot,
          Status = cycle.Status.ToString(),
          cycle.CreatedAt
        });
      }
      catch (KeyNotFoundException)
      {
        return ResultUtils.Fail("NOT_FOUND", $"Chu kỳ lương '{monthKey}' chưa được tạo.", 404);
      }
    }

    /// <summary>
    /// PUT /api/payroll-cycles/{monthKey}/close — Đóng/chốt chu kỳ lương.
    /// </summary>
    public static async Task<IResult> Close(
        string monthKey,
        IPayrollCycleService cycleService,
        IPayrollCycleRepository repo)
    {
      var cycle = await repo.GetByMonthKeyAsync(monthKey);
      if (cycle is null)
        return ResultUtils.Fail("NOT_FOUND", $"Chu kỳ lương '{monthKey}' không tồn tại.", 404);

      try { cycle.Close(); }
      catch (InvalidOperationException ex)
      {
        return ResultUtils.Fail("INVALID_STATE", ex.Message, 409);
      }

      await repo.UpdateAsync(cycle.Id, cycle);
      return ResultUtils.Success($"Chu kỳ lương {monthKey} đã được chốt.");
    }

    /// <summary>
    /// PUT /api/payroll-cycles/{monthKey}/cancel — Hủy chu kỳ lương.
    /// </summary>
    public static async Task<IResult> Cancel(
        string monthKey,
        IPayrollCycleService cycleService,
        IPayrollCycleRepository repo)
    {
      var cycle = await repo.GetByMonthKeyAsync(monthKey);
      if (cycle is null)
        return ResultUtils.Fail("NOT_FOUND", $"Chu kỳ lương '{monthKey}' không tồn tại.", 404);

      try { cycle.Cancel(); }
      catch (InvalidOperationException ex)
      {
        return ResultUtils.Fail("INVALID_STATE", ex.Message, 409);
      }

      await repo.UpdateAsync(cycle.Id, cycle);
      return ResultUtils.Success($"Chu kỳ lương {monthKey} đã bị hủy.");
    }
  }
}
