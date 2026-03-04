using Carter;
using Employee.API.Common;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Payroll
{
  // ── Request / Response DTOs ───────────────────────────────────────────────
  public record UpsertPublicHolidayRequest(
      DateTime Date,
      string Name,
      bool IsRecurringYearly = false,
      string? Note = null);

  // ── Carter Module ─────────────────────────────────────────────────────────
  public class PublicHolidayModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/public-holidays")
                     .WithTags("Public Holidays")
                     .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      group.MapGet("/", PublicHolidayHandlers.GetAll);
      group.MapGet("/year/{year:int}", PublicHolidayHandlers.GetByYear);
      group.MapPost("/", PublicHolidayHandlers.Create);
      group.MapPut("/{id}", PublicHolidayHandlers.Update);
      group.MapDelete("/{id}", PublicHolidayHandlers.Delete);
    }
  }

  // ── Handlers ─────────────────────────────────────────────────────────────
  public static class PublicHolidayHandlers
  {
    /// <summary>GET /api/public-holidays — Lấy tất cả ngày lễ (không phân trang)</summary>
    public static async Task<IResult> GetAll(IPublicHolidayRepository repo)
    {
      var holidays = await repo.GetAllAsync();
      var ordered = holidays.OrderBy(h => h.Date).ThenBy(h => h.Name);
      return ResultUtils.Success(ordered);
    }

    /// <summary>GET /api/public-holidays/year/{year} — Lấy ngày lễ của một năm</summary>
    public static async Task<IResult> GetByYear(int year, IPublicHolidayRepository repo)
    {
      if (year < 2000 || year > 2100)
        return ResultUtils.Fail("INVALID_YEAR", "Năm không hợp lệ (2000–2100).", 400);

      var holidays = await repo.GetByYearAsync(year);
      var ordered = holidays.OrderBy(h => h.Date).ThenBy(h => h.Name);
      return ResultUtils.Success(ordered);
    }

    /// <summary>POST /api/public-holidays — Tạo ngày lễ mới</summary>
    public static async Task<IResult> Create(
        [FromBody] UpsertPublicHolidayRequest request,
        IPublicHolidayRepository repo)
    {
      if (string.IsNullOrWhiteSpace(request.Name))
        return ResultUtils.Fail("VALIDATION_ERROR", "Tên ngày lễ không được để trống.", 400);

      var holiday = new PublicHoliday(request.Date, request.Name, request.IsRecurringYearly, request.Note);
      await repo.CreateAsync(holiday);
      return ResultUtils.Success(holiday, "Đã tạo ngày lễ thành công.");
    }

    /// <summary>PUT /api/public-holidays/{id} — Cập nhật ngày lễ</summary>
    public static async Task<IResult> Update(
        string id,
        [FromBody] UpsertPublicHolidayRequest request,
        IPublicHolidayRepository repo)
    {
      var holiday = await repo.GetByIdAsync(id);
      if (holiday is null)
        return ResultUtils.Fail("NOT_FOUND", $"Không tìm thấy ngày lễ với id '{id}'.", 404);

      if (string.IsNullOrWhiteSpace(request.Name))
        return ResultUtils.Fail("VALIDATION_ERROR", "Tên ngày lễ không được để trống.", 400);

      holiday.Update(request.Date, request.Name, request.IsRecurringYearly, request.Note);
      await repo.UpdateAsync(id, holiday);
      return ResultUtils.Success(holiday, "Đã cập nhật ngày lễ thành công.");
    }

    /// <summary>DELETE /api/public-holidays/{id} — Xóa mềm ngày lễ</summary>
    public static async Task<IResult> Delete(string id, IPublicHolidayRepository repo)
    {
      var holiday = await repo.GetByIdAsync(id);
      if (holiday is null)
        return ResultUtils.Fail("NOT_FOUND", $"Không tìm thấy ngày lễ với id '{id}'.", 404);

      await repo.DeleteAsync(id);
      return ResultUtils.Success("Đã xóa ngày lễ thành công.");
    }
  }
}
