using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Threading.Tasks;

namespace Employee.Application.Common.Services
{
  public class PayslipService : IPayslipService
  {
    private readonly IPayrollRepository _payrollRepo;

    public PayslipService(IPayrollRepository payrollRepo)
    {
      _payrollRepo = payrollRepo;
      // Set QuestPDF license (Community for free/open-source usage)
      QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]?> GeneratePayslipPdfAsync(string payrollId)
    {
      var payroll = await _payrollRepo.GetByIdAsync(payrollId);
      if (payroll == null) return null;

      var document = Document.Create(container =>
      {
        container.Page(page =>
              {
            page.Size(PageSizes.A4);
            page.Margin(1, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                // 1. Header
            page.Header().Row(row =>
                  {
                row.RelativeItem().Column(col =>
                      {
                    col.Item().Text("PAYSLIP / PHIẾU LƯƠNG").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{payroll.Month}").FontSize(14);
                  });

                row.RelativeItem().AlignRight().Column(col =>
                      {
                    col.Item().Text("Employee HRMS").SemiBold();
                    col.Item().Text("123 Clean Architecture St.");
                    col.Item().Text("Ho Chi Minh City, Vietnam");
                  });
              });

                // 2. Employee Info
            page.Content().PaddingVertical(20).Column(col =>
                  {
                col.Item().BorderBottom(1).PaddingBottom(5).Text("EMPLOYEE INFORMATION / THÔNG TIN NHÂN VIÊN").SemiBold();
                col.Item().Row(row =>
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
                col.Item().Row(row =>
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

                    // 3. Salary Table
                col.Item().PaddingTop(20).Table(table =>
                      {
                    table.ColumnsDefinition(columns =>
                          {
                        columns.RelativeColumn();
                        columns.ConstantColumn(100);
                      });

                        // Table Header
                    table.Header(header =>
                          {
                        header.Cell().Element(CellStyle).Text("Description / Diễn giải");
                        header.Cell().Element(CellStyle).AlignRight().Text("Amount / Số tiền");

                        static IContainer CellStyle(IContainer container)
                        {
                          return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                      });

                        // Income Section
                    table.Cell().Element(RowStyle).Text("Basic Salary / Lương cơ bản");
                    table.Cell().Element(RowStyle).AlignRight().Text(payroll.BaseSalary.ToString("N0"));

                    table.Cell().Element(RowStyle).Text("Allowances / Phụ cấp");
                    table.Cell().Element(RowStyle).AlignRight().Text(payroll.Allowances.ToString("N0"));

                    table.Cell().Element(RowStyle).Text($"Overtime / Tăng ca ({payroll.OvertimeHours}h)");
                    table.Cell().Element(RowStyle).AlignRight().Text(payroll.OvertimePay.ToString("N0"));

                    table.Cell().Element(RowStyle).Text("Bonus / Thưởng");
                    table.Cell().Element(RowStyle).AlignRight().Text(payroll.Bonus.ToString("N0"));

                    table.Cell().PaddingVertical(5).Text("GROSS INCOME / TỔNG THU NHẬP").SemiBold();
                    table.Cell().PaddingVertical(5).AlignRight().Text(payroll.GrossIncome.ToString("N0")).SemiBold();

                        // Deductions Section
                    table.Cell().Element(RowStyle).Text("Social Insurance / BHXH (8%)").FontColor(Colors.Red.Medium);
                    table.Cell().Element(RowStyle).AlignRight().Text($"-{payroll.SocialInsurance:N0}").FontColor(Colors.Red.Medium);

                    table.Cell().Element(RowStyle).Text("Health Insurance / BHYT (1.5%)").FontColor(Colors.Red.Medium);
                    table.Cell().Element(RowStyle).AlignRight().Text($"-{payroll.HealthInsurance:N0}").FontColor(Colors.Red.Medium);

                    table.Cell().Element(RowStyle).Text("Unemployment Insurance / BHTN (1%)").FontColor(Colors.Red.Medium);
                    table.Cell().Element(RowStyle).AlignRight().Text($"-{payroll.UnemploymentInsurance:N0}").FontColor(Colors.Red.Medium);

                    table.Cell().Element(RowStyle).Text("Personal Income Tax / Thuế TNCN").FontColor(Colors.Red.Medium);
                    table.Cell().Element(RowStyle).AlignRight().Text($"-{payroll.PersonalIncomeTax:N0}").FontColor(Colors.Red.Medium);

                    if (payroll.DebtPaid > 0)
                    {
                      table.Cell().Element(RowStyle).Text("Debt Repayment / Hoàn trả nợ").FontColor(Colors.Red.Medium);
                      table.Cell().Element(RowStyle).AlignRight().Text($"-{payroll.DebtPaid:N0}").FontColor(Colors.Red.Medium);
                    }

                        // Net Salary
                    table.Cell().PaddingVertical(10).BorderTop(1).Text("NET SALARY / THỰC LĨNH").FontSize(14).SemiBold();
                    table.Cell().PaddingVertical(10).BorderTop(1).AlignRight().Text(payroll.FinalNetSalary.ToString("N0")).FontSize(14).SemiBold().FontColor(Colors.Green.Medium);

                    static IContainer RowStyle(IContainer container)
                    {
                      return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                  });

                col.Item().PaddingTop(30).Row(row =>
                      {
                    row.RelativeItem().Column(c =>
                          {
                        c.Item().AlignCenter().Text("Employee's Signature / Chữ ký nhân viên");
                        c.Item().PaddingTop(40).AlignCenter().Text("(Signed)");
                      });
                    row.RelativeItem().Column(c =>
                          {
                        c.Item().AlignCenter().Text("Employer's Signature / Chữ ký người sử dụng lao động");
                        c.Item().PaddingTop(40).AlignCenter().Text("(Signed & Sealed)");
                      });
                  });
              });

            page.Footer().AlignCenter().Text(x =>
                  {
                x.Span("Page ");
                x.CurrentPageNumber();
              });
          });
      });

      return document.GeneratePdf();
    }
  }
}
