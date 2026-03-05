using Employee.Application.Common.Interfaces;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Employee.Application.Common.Services
{
  public class PayslipService : IPayslipService
  {
    private readonly IPayrollRepository _payrollRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly ILeaveRequestRepository _leaveRepo;

    public PayslipService(
        IPayrollRepository payrollRepo,
        IAttendanceRepository attendanceRepo,
        ILeaveRequestRepository leaveRepo)
    {
      _payrollRepo = payrollRepo;
      _attendanceRepo = attendanceRepo;
      _leaveRepo = leaveRepo;
      QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]?> GeneratePayslipPdfAsync(string payrollId)
    {
      var payroll = await _payrollRepo.GetByIdAsync(payrollId);
      if (payroll == null) return null;

      // Parse "MM-YYYY" month key
      var parts = payroll.Month.Split('-');
      int monthNum = int.Parse(parts[0]);
      int yearNum = int.Parse(parts[1]);
      var monthStart = new DateTime(yearNum, monthNum, 1);
      var monthEnd = monthStart.AddMonths(1).AddDays(-1);

      // Fetch attendance bucket + leave requests concurrently
      var bucketTask = _attendanceRepo.GetByEmployeeAndMonthAsync(payroll.EmployeeId, payroll.Month);
      var leavesTask = _leaveRepo.GetByEmployeeIdAsync(payroll.EmployeeId);
      await Task.WhenAll(bucketTask, leavesTask);

      var bucket = bucketTask.Result;
      var allLeaves = leavesTask.Result ?? new List<LeaveRequest>();

      // Approved leaves overlapping this payroll month
      var approvedLeaves = allLeaves
          .Where(l => l.Status == LeaveStatus.Approved
                   && l.FromDate.Date <= monthEnd
                   && l.ToDate.Date >= monthStart)
          .OrderBy(l => l.FromDate)
          .ToList();

      var dailyLogs = (bucket?.DailyLogs ?? new List<DailyLog>())
          .OrderBy(d => d.Date)
          .ToList();

      var document = Document.Create(container =>
      {
        // ══════════════════════════════════════════════════════════════
        // PAGE 1 – Employee Info + Salary Breakdown + Attendance Summary
        // ══════════════════════════════════════════════════════════════
        container.Page(page =>
        {
          page.Size(PageSizes.A4);
          page.Margin(1.5f, Unit.Centimetre);
          page.PageColor(Colors.White);
          page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Verdana));

          page.Header().PaddingBottom(8).Row(row =>
          {
            row.RelativeItem().Column(col =>
            {
              col.Item().Text("PAYSLIP / PHIẾU LƯƠNG")
                  .FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
              col.Item().Text(payroll.Month).FontSize(12).FontColor(Colors.Grey.Darken2);
            });
            row.RelativeItem().AlignRight().Column(col =>
            {
              col.Item().Text("Employee HRMS").SemiBold();
              col.Item().Text("123 Clean Architecture St.");
              col.Item().Text("Ho Chi Minh City, Vietnam");
            });
          });

          page.Content().Column(col =>
          {
            // ── 1. Employee Info ───────────────────────────────────────
            col.Item().BorderBottom(1).PaddingBottom(4)
                .Text("EMPLOYEE INFORMATION / THÔNG TIN NHÂN VIÊN")
                .SemiBold().FontSize(10);

            col.Item().PaddingVertical(5).Row(row =>
            {
              row.RelativeItem().Text(t =>
              {
                t.Span("Name / Họ tên: ").SemiBold();
                t.Span(payroll.Snapshot.EmployeeName);
              });
              row.RelativeItem().Text(t =>
              {
                t.Span("Code / Mã NV: ").SemiBold();
                t.Span(payroll.Snapshot.EmployeeCode);
              });
            });
            col.Item().PaddingBottom(8).Row(row =>
            {
              row.RelativeItem().Text(t =>
              {
                t.Span("Department / Phòng ban: ").SemiBold();
                t.Span(payroll.Snapshot.DepartmentName);
              });
              row.RelativeItem().Text(t =>
              {
                t.Span("Position / Chức danh: ").SemiBold();
                t.Span(payroll.Snapshot.PositionTitle);
              });
            });

            // ── 2. Payroll Cycle Info ──────────────────────────────────
            col.Item().Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten3)
                .Padding(6).PaddingBottom(8).Row(row =>
            {
              row.RelativeItem().Text(t =>
              {
                t.Span("Payroll Period / Kỳ lương: ").SemiBold();
                t.Span(payroll.Month);
              });
              row.RelativeItem().Text(t =>
              {
                t.Span("Status / Trạng thái: ").SemiBold();
                t.Span(payroll.Status.ToString());
              });
              row.RelativeItem().Text(t =>
              {
                t.Span("Paid Date / Ngày trả: ").SemiBold();
                t.Span(payroll.PaidDate.HasValue ? payroll.PaidDate.Value.ToString("dd/MM/yyyy") : "-");
              });
            });

            col.Item().PaddingTop(8).BorderBottom(1).PaddingBottom(4)
                .Text("SALARY DETAILS / CHI TIẾT LƯƠNG").SemiBold().FontSize(10);

            // ── 3. Salary Table ────────────────────────────────────────
            col.Item().PaddingBottom(8).Table(table =>
            {
              table.ColumnsDefinition(c =>
              {
                c.RelativeColumn();
                c.ConstantColumn(120);
              });

              table.Header(h =>
              {
                h.Cell().Element(HeaderCell).Text("Description / Diễn giải");
                h.Cell().Element(HeaderCell).AlignRight().Text("Amount (VNĐ) / Số tiền");
              });

              // Income
              SalaryRow(table, "Basic Salary / Lương cơ bản", payroll.BaseSalary.ToString("N0"));
              SalaryRow(table, "Allowances / Phụ cấp", payroll.Allowances.ToString("N0"));
              SalaryRow(table, $"Overtime / Tăng ca ({payroll.OvertimeHours:F2}h × 1.5× daily rate)", payroll.OvertimePay.ToString("N0"));
              SalaryRow(table, "Bonus / Thưởng", payroll.Bonus.ToString("N0"));

              // Working days formula note
              table.Cell().ColumnSpan(2)
                  .Background(Colors.Green.Lighten5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                  .PaddingVertical(4).PaddingHorizontal(6).Text(t =>
                  {
                    t.Span("Formula / Công thức: ").SemiBold().FontSize(8);
                    t.Span($"(BaseSalary + Allowances) ÷ {payroll.TotalWorkingDays:F0} std.days × {payroll.PayableDays:F2} payable days + OT")
                        .FontSize(8).FontColor(Colors.Grey.Darken2);
                  });

              table.Cell().PaddingVertical(5).Text("GROSS INCOME / TỔNG THU NHẬP").SemiBold();
              table.Cell().PaddingVertical(5).AlignRight().Text(payroll.GrossIncome.ToString("N0")).SemiBold();

              // Deductions
              DeductionRow(table, $"Social Insurance / BHXH (8% × capped salary)", payroll.SocialInsurance);
              DeductionRow(table, $"Health Insurance / BHYT (1.5% × capped salary)", payroll.HealthInsurance);
              DeductionRow(table, $"Unemployment Ins. / BHTN (1% × capped salary)", payroll.UnemploymentInsurance);
              DeductionRow(table, "Personal Income Tax / Thuế TNCN (progressive)", payroll.PersonalIncomeTax);
              if (payroll.DebtPaid > 0)
                DeductionRow(table, "Debt Repayment / Hoàn trả nợ (carry-forward)", payroll.DebtPaid);

              table.Cell().PaddingVertical(8).BorderTop(1).Text("NET SALARY / THỰC LĨNH")
                  .FontSize(13).SemiBold();
              table.Cell().PaddingVertical(8).BorderTop(1).AlignRight()
                  .Text(payroll.FinalNetSalary.ToString("N0")).FontSize(13).SemiBold().FontColor(Colors.Green.Medium);

              if (payroll.DebtAmount > 0)
              {
                table.Cell().ColumnSpan(2)
                    .Background(Colors.Orange.Lighten4).BorderBottom(1).BorderColor(Colors.Orange.Lighten2)
                    .PaddingVertical(4).PaddingHorizontal(6).Text(t =>
                    {
                      t.Span("⚠ Carry-forward debt / Nợ chuyển sang tháng sau: ").SemiBold().FontSize(8).FontColor(Colors.Orange.Darken2);
                      t.Span($"{payroll.DebtAmount:N0} VNĐ").SemiBold().FontSize(8).FontColor(Colors.Red.Darken1);
                    });
              }
            });

            // ── 4. Attendance Summary Tiles ────────────────────────────
            col.Item().PaddingTop(6).BorderBottom(1).PaddingBottom(4)
                .Text("ATTENDANCE SUMMARY / TỔNG HỢP CHẤM CÔNG").SemiBold().FontSize(10);

            col.Item().PaddingBottom(8).Table(table =>
            {
              table.ColumnsDefinition(c =>
              {
                c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
              });

              SummaryTile(table, "Std. Working Days\nNgày công chuẩn",
                  payroll.TotalWorkingDays.ToString("F0") + " days");
              SummaryTile(table, "Days Present\nNgày thực chấm công",
                  payroll.ActualWorkingDays.ToString("F1") + " days", Colors.Green.Darken1);
              SummaryTile(table, "Payable Days\nCông tính lương",
                  payroll.PayableDays.ToString("F2") + " days", Colors.Blue.Darken1);
              SummaryTile(table, "Unpaid Leave\nPhép không lương",
                  payroll.UnpaidLeaveDays.ToString("F1") + " days",
                  payroll.UnpaidLeaveDays > 0 ? Colors.Red.Darken1 : (string?)null);
              SummaryTile(table, "Total OT Hours\nGiờ tăng ca",
                  $"{payroll.OvertimeHours:F2}h", Colors.Purple.Darken1);
              SummaryTile(table, "Late Days\nNgày đi trễ",
                  (bucket?.TotalLate ?? 0).ToString(),
                  (bucket?.TotalLate ?? 0) > 0 ? Colors.Orange.Darken1 : (string?)null);
            });

            // ── 5. Signature Block ─────────────────────────────────────
            col.Item().PaddingTop(16).Row(row =>
            {
              row.RelativeItem().Column(c =>
              {
                c.Item().AlignCenter().Text("Employee's Signature / Chữ ký nhân viên").FontSize(9);
                c.Item().PaddingTop(35).AlignCenter().Text("(Signed)").FontColor(Colors.Grey.Medium);
              });
              row.RelativeItem().Column(c =>
              {
                c.Item().AlignCenter().Text("Employer's Signature / Chữ ký người sử dụng lao động").FontSize(9);
                c.Item().PaddingTop(35).AlignCenter().Text("(Signed & Sealed)").FontColor(Colors.Grey.Medium);
              });
            });
          });

          page.Footer().AlignCenter().Text(x =>
          {
            x.Span("Page "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages();
          });
        });

        // ══════════════════════════════════════════════════════════════
        // PAGE 2 – Daily Attendance Log
        // ══════════════════════════════════════════════════════════════
        if (dailyLogs.Count > 0)
        {
          container.Page(page =>
          {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

            page.Header().PaddingBottom(8).Column(col =>
            {
              col.Item().Text("ATTENDANCE LOG / BẢNG CHẤM CÔNG CHI TIẾT")
                  .FontSize(13).SemiBold().FontColor(Colors.Blue.Medium);
              col.Item().Text(
                  $"Period / Kỳ: {payroll.Month}   ·   " +
                  $"Employee / NV: {payroll.Snapshot.EmployeeName} ({payroll.Snapshot.EmployeeCode})   ·   " +
                  $"Dept: {payroll.Snapshot.DepartmentName}")
                  .FontSize(8).FontColor(Colors.Grey.Darken2);
            });

            page.Content().Table(table =>
            {
              table.ColumnsDefinition(c =>
              {
                c.ConstantColumn(62);  // Date
                c.ConstantColumn(30);  // Day
                c.ConstantColumn(32);  // Shift
                c.ConstantColumn(48);  // Check-In
                c.ConstantColumn(48);  // Check-Out
                c.ConstantColumn(36);  // Worked
                c.ConstantColumn(30);  // OT
                c.ConstantColumn(60);  // Status
                c.RelativeColumn();    // Violations / Note
              });

              table.Header(h =>
              {
                Th(h, "Date / Ngày");
                Th(h, "Day");
                Th(h, "Shift");
                Th(h, "Check-In");
                Th(h, "Check-Out");
                Th(h, "Worked");
                Th(h, "OT");
                Th(h, "Status");
                Th(h, "Violations / Note");
              });

              foreach (var log in dailyLogs)
              {
                var bg = (log.IsWeekend || log.IsHoliday)
                    ? Colors.Grey.Lighten3 : Colors.White;

                string statusText;
                string statusColor;
#pragma warning disable CS0618
                if (log.IsHoliday) { statusText = "Holiday"; statusColor = Colors.Purple.Darken1; }
                else if (log.IsWeekend) { statusText = "Weekend"; statusColor = Colors.Grey.Darken1; }
                else if (log.Status == AttendanceStatus.Leave) { statusText = "Leave"; statusColor = Colors.Blue.Darken1; }
                else if (log.Status == AttendanceStatus.Present
                      || log.Status == AttendanceStatus.Late
                      || log.Status == AttendanceStatus.EarlyLeave)
                { statusText = "Present"; statusColor = Colors.Green.Darken1; }
                else { statusText = "Absent"; statusColor = Colors.Red.Darken1; }
#pragma warning restore CS0618

                var flags = new List<string>();
                if (log.IsLate) flags.Add($"Late +{log.LateMinutes}m");
                if (log.IsEarlyLeave) flags.Add($"Early -{log.EarlyLeaveMinutes}m");
                if (log.IsMissingPunch) flags.Add("No punch-out");
                var violationNote = string.Join(" · ", flags);
                if (!string.IsNullOrWhiteSpace(log.Note))
                  violationNote = string.IsNullOrWhiteSpace(violationNote)
                      ? log.Note : $"{violationNote} · {log.Note}";

                LogTd(table, log.Date.ToString("dd/MM/yyyy"), bg);
                LogTd(table, log.Date.ToString("ddd"), bg);
                LogTd(table, string.IsNullOrEmpty(log.ShiftCode) ? "-" : log.ShiftCode, bg);
                LogTd(table, log.CheckIn.HasValue ? log.CheckIn.Value.ToString("HH:mm") : "-", bg);
                LogTd(table, log.CheckOut.HasValue ? log.CheckOut.Value.ToString("HH:mm") : "-", bg);
                LogTd(table, log.WorkingHours > 0 ? $"{log.WorkingHours:F1}h" : "-", bg);
                LogTd(table, log.OvertimeHours > 0 ? $"{log.OvertimeHours:F1}h" : "-", bg);
                table.Cell().Background(bg).BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2).Padding(3)
                    .Text(statusText).FontColor(statusColor).SemiBold();
                table.Cell().Background(bg).BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2).Padding(3)
                    .Text(violationNote).FontColor(
                        flags.Count > 0 ? Colors.Orange.Darken2 : Colors.Grey.Darken1);
              }
            });

            // Summary footer row
            page.Content().PaddingTop(6).Table(table =>
            {
              table.ColumnsDefinition(c =>
              {
                c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
              });
              SummaryTile(table, "Total Present\nTổng ngày có mặt",
                  (bucket?.TotalPresent ?? 0).ToString() + " days", Colors.Green.Darken1);
              SummaryTile(table, "Total Late\nNgày đi trễ",
                  (bucket?.TotalLate ?? 0).ToString() + " days",
                  (bucket?.TotalLate ?? 0) > 0 ? Colors.Orange.Darken1 : (string?)null);
              SummaryTile(table, "Total OT\nTổng tăng ca",
                  $"{(bucket?.TotalOvertime ?? 0):F2}h", Colors.Purple.Darken1);
              SummaryTile(table, "Leave Days (this month)\nNgày nghỉ phép",
                  dailyLogs.Count(l => l.Status == AttendanceStatus.Leave).ToString() + " days",
                  Colors.Blue.Darken1);
            });

            page.Footer().AlignCenter().Text(x =>
            {
              x.Span("Page "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages();
            });
          });
        }

        // ══════════════════════════════════════════════════════════════
        // PAGE 3 – Approved Leave Requests
        // ══════════════════════════════════════════════════════════════
        if (approvedLeaves.Count > 0)
        {
          container.Page(page =>
          {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Verdana));

            page.Header().PaddingBottom(8).Column(col =>
            {
              col.Item().Text("APPROVED LEAVE REQUESTS / ĐƠN NGHỈ PHÉP ĐÃ DUYỆT")
                  .FontSize(13).SemiBold().FontColor(Colors.Blue.Medium);
              col.Item().Text(
                  $"Period / Kỳ: {payroll.Month}  ·  " +
                  $"{payroll.Snapshot.EmployeeName} ({payroll.Snapshot.EmployeeCode})")
                  .FontSize(8).FontColor(Colors.Grey.Darken2);
            });

            page.Content().Column(col =>
            {
              // Leave table
              col.Item().Table(table =>
              {
                table.ColumnsDefinition(c =>
                {
                  c.ConstantColumn(70);   // Type
                  c.ConstantColumn(80);   // From
                  c.ConstantColumn(80);   // To
                  c.ConstantColumn(40);   // Days
                  c.RelativeColumn();     // Reason
                  c.ConstantColumn(70);   // Approved By
                });

                table.Header(h =>
                {
                  Th(h, "Type / Loại");
                  Th(h, "From / Từ ngày");
                  Th(h, "To / Đến ngày");
                  Th(h, "Days");
                  Th(h, "Reason / Lý do");
                  Th(h, "Approved By");
                });

                foreach (var leave in approvedLeaves)
                {
                  double leaveDays = Math.Round((leave.ToDate - leave.FromDate).TotalDays + 1, 1);
                  // Clip to payroll month for display accuracy
                  var clippedFrom = leave.FromDate.Date < monthStart ? monthStart : leave.FromDate.Date;
                  var clippedTo = leave.ToDate.Date > monthEnd ? monthEnd : leave.ToDate.Date;
                  double payrollMonthDays = Math.Round((clippedTo - clippedFrom).TotalDays + 1, 1);

                  (string typeStr, string typeColor) = leave.LeaveType switch
                  {
                    LeaveCategory.Annual => ("Annual / Phép năm", Colors.Green.Darken1),
                    LeaveCategory.Sick => ("Sick / Phép ốm", Colors.Orange.Darken1),
                    LeaveCategory.Unpaid => ("Unpaid / Không lương", Colors.Red.Darken1),
                    _ => (leave.LeaveType.ToString(), Colors.Black)
                  };

                  table.Cell().Element(TdBase).Text(typeStr).FontColor(typeColor).SemiBold();
                  LogTd(table, leave.FromDate.ToString("dd/MM/yyyy"), Colors.White);
                  LogTd(table, leave.ToDate.ToString("dd/MM/yyyy"), Colors.White);
                  table.Cell().Element(TdBase).Text(t =>
                  {
                    t.Line($"{leaveDays:F0} day(s)").SemiBold();
                    if (Math.Abs(payrollMonthDays - leaveDays) > 0.01)
                      t.Line($"({payrollMonthDays:F0} in month)")
                          .FontColor(Colors.Blue.Darken1).FontSize(7);
                  });
                  LogTd(table, leave.Reason ?? "", Colors.White);
                  LogTd(table, leave.ApprovedBy ?? "-", Colors.White);
                }
              });

              // Leave type summary tiles
              col.Item().PaddingTop(14).BorderBottom(1).PaddingBottom(4)
                  .Text("LEAVE SUMMARY / TỔNG HỢP PHÉP").SemiBold().FontSize(10);

              col.Item().PaddingTop(6).Table(table =>
              {
                table.ColumnsDefinition(c =>
                {
                  c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                });

                foreach (var (type, label, color) in new[]
                {
                  (LeaveCategory.Annual,  "Annual Leave / Phép năm",        Colors.Green.Darken1),
                  (LeaveCategory.Sick,    "Sick Leave / Phép ốm",           Colors.Orange.Darken1),
                  (LeaveCategory.Unpaid,  "Unpaid Leave / Không lương",      Colors.Red.Darken1)
                })
                {
                  var matching = approvedLeaves.Where(l => l.LeaveType == type).ToList();
                  double totalDays = matching.Sum(l =>
                  {
                    var cf = l.FromDate.Date < monthStart ? monthStart : l.FromDate.Date;
                    var ct = l.ToDate.Date > monthEnd ? monthEnd : l.ToDate.Date;
                    return Math.Round((ct - cf).TotalDays + 1, 1);
                  });
                  SummaryTile(table, label,
                      $"{matching.Count} request(s) · {totalDays:F0} day(s) in month",
                      matching.Count > 0 ? color : (string?)null);
                }
              });

              // Payroll impact note
              col.Item().PaddingTop(14).Background(Colors.Orange.Lighten5)
                  .Border(1).BorderColor(Colors.Orange.Lighten3).Padding(8).Column(inner =>
              {
                inner.Item().Text("PAYROLL IMPACT NOTE / GHI CHÚ TÁC ĐỘNG LƯƠNG")
                    .SemiBold().FontSize(9).FontColor(Colors.Orange.Darken2);
                inner.Item().PaddingTop(4).Text(t =>
                {
                  t.Line("• Annual / Sick leave (Phép năm / Phép ốm): counted as payable days → no salary deduction.")
                      .FontSize(8);
                  t.Line("• Unpaid leave (Phép không lương): deducted from payable days → reduces gross salary proportionally.")
                      .FontSize(8);
                  t.Line($"• Unpaid leave days this period: {payroll.UnpaidLeaveDays:F1} day(s) deducted from salary base.")
                      .FontSize(8).FontColor(Colors.Red.Darken1);
                });
              });
            });

            page.Footer().AlignCenter().Text(x =>
            {
              x.Span("Page "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages();
            });
          });
        }
      });

      return document.GeneratePdf();
    }

    // ─── Shared Style Helpers ──────────────────────────────────────────────────

    private static IContainer HeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(8))
         .Background(Colors.Grey.Lighten3)
         .PaddingVertical(5).PaddingHorizontal(4)
         .BorderBottom(1).BorderColor(Colors.Black);

    private static IContainer TdBase(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3);

    private static void SalaryRow(TableDescriptor t, string label, string amount)
    {
      t.Cell().Element(TdBase).Text(label);
      t.Cell().Element(TdBase).AlignRight().Text(amount);
    }

    private static void DeductionRow(TableDescriptor t, string label, decimal amount)
    {
      t.Cell().Element(TdBase).Text(label).FontColor(Colors.Red.Medium);
      t.Cell().Element(TdBase).AlignRight().Text($"-{amount:N0}").FontColor(Colors.Red.Medium);
    }

    private static void Th(TableCellDescriptor h, string text) =>
        h.Cell().Element(HeaderCell).AlignCenter().Text(text);

    private static void LogTd(TableDescriptor t, string text, string bg) =>
        t.Cell().Background(bg).BorderBottom(1)
         .BorderColor(Colors.Grey.Lighten2).Padding(3).Text(text);

    private static void SummaryTile(TableDescriptor t, string label, string value, string? color = null)
    {
      t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(inner =>
      {
        inner.Item().Text(label.Replace("\n", " / ")).FontSize(7).FontColor(Colors.Grey.Darken2);
        if (color != null)
          inner.Item().Text(value).SemiBold().FontSize(10).FontColor(color);
        else
          inner.Item().Text(value).SemiBold().FontSize(10);
      });
    }
  }
}
