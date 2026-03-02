using Employee.API.Common;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Common.Models;
using Employee.Application.Features.Payroll.Dtos;
using Employee.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Employee.Application.Features.Payroll.Commands.GeneratePayroll;
using Employee.Application.Features.Payroll.Commands.MarkPayrollPaid;
using Microsoft.AspNetCore.Http;

namespace Employee.API.Endpoints.Payroll
{
  public static class PayrollHandlers
  {
    public static async Task<IResult> GetPagedList(
      [FromBody] PaginationParams pagination,
      IPayrollService service)
    {
      var result = await service.GetPagedListAsync(pagination);
      return ResultUtils.Success(result, "Retrieved paginated payroll list successfully.");
    }

    // 1. GET MY HISTORY
    public static async Task<IResult> GetMyHistory(
        ICurrentUser currentUser,
        IPayrollService service)
    {
      var list = await service.GetMyHistoryAsync(currentUser.EmployeeId ?? currentUser.UserId);
      return ResultUtils.Success(list);
    }

    // 2. GET BY ID (Chi ti?t 1 phi?u luong)
    public static async Task<IResult> GetById(string id, IPayrollService service, ICurrentUser currentUser)
    {
      var item = await service.GetByIdAsync(id);
      // Employee ch? du?c xem phi?u luong c?a ch�nh m�nh; Admin/HR du?c xem t?t c?
      if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("HR"))
      {
        var employeeId = currentUser.EmployeeId ?? currentUser.UserId;
        if (item.EmployeeId != employeeId)
          return ResultUtils.Fail("PAYROLL_FORBIDDEN", "B?n kh�ng c� quy?n xem phi?u luong n�y.", 403);
      }
      return ResultUtils.Success(item);
    }

    // 3. GET BY MONTH (Admin/HR xem b?ng t?ng h?p)
    public static async Task<IResult> GetByMonth(
        [FromQuery] string month,
        [AsParameters] PaginationParams pagination,
        IPayrollService service)
    {
      // N?u kh�ng truy?n month, l?y th�ng hi?n t?i
      if (string.IsNullOrEmpty(month)) month = DateTime.UtcNow.ToString("MM-yyyy");

      var list = await service.GetByMonthPagedAsync(month, pagination);
      return ResultUtils.Success(list);
    }

    // 4. GENERATE PAYROLL (T�nh luong - CQRS)
    public static async Task<IResult> Generate(
        [FromBody] GeneratePayrollDto dto,
        ISender sender)
    {
      var command = new GeneratePayrollCommand
      {
        Month = dto.Month
        // EmployeeIds mapping if needed
      };

      int count = await sender.Send(command);

      return ResultUtils.Success($"Payroll calculation completed. Processed {count} records via CQRS.");
    }

    // 5. UPDATE STATUS (Duy?t/Thanh to�n - CQRS)
    public static async Task<IResult> UpdateStatus(
        string id,
        [FromBody] UpdatePayrollStatusDto dto,
        ISender sender)
    {
      if (id != dto.Id) return ResultUtils.Fail(ErrorCodes.InvalidData, "ID mismatch");

      var command = new UpdatePayrollStatusCommand
      {
        Id = id,
        Status = dto.Status
      };

      await sender.Send(command);
      return ResultUtils.Success($"Payroll status updated via CQRS.");
    }

    public static async Task<IResult> GetPdf(string id, IPayslipService service, IPayrollService payrollService, ICurrentUser currentUser)
    {
      // Employee ch? du?c t?i payslip c?a ch�nh m�nh; Admin/HR du?c t?i t?t c?
      if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("HR"))
      {
        var payroll = await payrollService.GetByIdAsync(id);
        var employeeId = currentUser.EmployeeId ?? currentUser.UserId;
        if (payroll.EmployeeId != employeeId)
          return ResultUtils.Fail("PAYSLIP_FORBIDDEN", "B?n kh�ng c� quy?n t?i payslip n�y.", 403);
      }
      var pdfBytes = await service.GeneratePayslipPdfAsync(id);
      if (pdfBytes == null) return ResultUtils.Fail("PAYSLIP_NOT_FOUND", $"Payslip PDF not found for payroll id '{id}'.", 404);

      return Results.File(pdfBytes, "application/pdf", $"Payslip_{id}.pdf");
    }

    public static async Task<IResult> ExportExcel([FromQuery] string month, IExcelExportService service)
    {
      if (string.IsNullOrEmpty(month)) month = DateTime.UtcNow.ToString("MM-yyyy");
      var excelBytes = await service.ExportPayrollToExcelAsync(month);

      return Results.File(
          excelBytes,
          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          $"Payroll_{month}.xlsx"
      );
    }

    /// <summary>NEW-7: Annual PIT tax report � aggregates all payroll records for a given year.</summary>
    public static async Task<IResult> GetTaxReport(int year, IPayrollService service)
    {
      if (year < 2020 || year > DateTime.UtcNow.Year + 1)
        return ResultUtils.Fail("INVALID_YEAR", $"Year must be between 2020 and {DateTime.UtcNow.Year + 1}.");

      var report = await service.GetAnnualTaxReportAsync(year);
      return ResultUtils.Success(report, $"Annual PIT tax report for {year} generated successfully.");
    }
    /// <summary>GET /api/payrolls/employee/{employeeId} — Admin/HR xem l?ch s? luong c?a m?t nh�n vi�n.</summary>
    public static async Task<IResult> GetByEmployeeId(string employeeId, IPayrollService service)
    {
      var list = await service.GetByEmployeeIdAsync(employeeId);
      return ResultUtils.Success(list);
    }
  }
}
