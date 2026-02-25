using ClosedXML.Excel;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using System.IO;
using System.Threading.Tasks;

namespace Employee.Application.Common.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly IPayrollRepository _payrollRepo;

        public ExcelExportService(IPayrollRepository payrollRepo)
        {
            _payrollRepo = payrollRepo;
        }

        public async Task<byte[]> ExportPayrollToExcelAsync(string month)
        {
            var payrolls = await _payrollRepo.GetByMonthAsync(month);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Payroll Summary");

                // Headers
                worksheet.Cell(1, 1).Value = "Employee Code";
                worksheet.Cell(1, 2).Value = "Full Name";
                worksheet.Cell(1, 3).Value = "Department";
                worksheet.Cell(1, 4).Value = "Position";
                worksheet.Cell(1, 5).Value = "Base Salary";
                worksheet.Cell(1, 6).Value = "Allowances";
                worksheet.Cell(1, 7).Value = "Overtime Pay";
                worksheet.Cell(1, 8).Value = "Bonus";
                worksheet.Cell(1, 9).Value = "Gross Income";
                worksheet.Cell(1, 10).Value = "Social Insurance";
                worksheet.Cell(1, 11).Value = "Health Insurance";
                worksheet.Cell(1, 12).Value = "Unemployment Insurance";
                worksheet.Cell(1, 13).Value = "Personal Income Tax";
                worksheet.Cell(1, 14).Value = "Debt Paid";
                worksheet.Cell(1, 15).Value = "Final Net Salary";
                worksheet.Cell(1, 16).Value = "Status";

                // Formatting Header
                var headerRange = worksheet.Range(1, 1, 1, 16);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Data
                int row = 2;
                foreach (var payroll in payrolls)
                {
                    worksheet.Cell(row, 1).Value = payroll.Snapshot.EmployeeCode;
                    worksheet.Cell(row, 2).Value = payroll.Snapshot.EmployeeName;
                    worksheet.Cell(row, 3).Value = payroll.Snapshot.DepartmentName;
                    worksheet.Cell(row, 4).Value = payroll.Snapshot.PositionTitle;
                    worksheet.Cell(row, 5).Value = payroll.BaseSalary;
                    worksheet.Cell(row, 6).Value = payroll.Allowances;
                    worksheet.Cell(row, 7).Value = payroll.OvertimePay;
                    worksheet.Cell(row, 8).Value = payroll.Bonus;
                    worksheet.Cell(row, 9).Value = payroll.GrossIncome;
                    worksheet.Cell(row, 10).Value = payroll.SocialInsurance;
                    worksheet.Cell(row, 11).Value = payroll.HealthInsurance;
                    worksheet.Cell(row, 12).Value = payroll.UnemploymentInsurance;
                    worksheet.Cell(row, 13).Value = payroll.PersonalIncomeTax;
                    worksheet.Cell(row, 14).Value = payroll.DebtPaid;
                    worksheet.Cell(row, 15).Value = payroll.FinalNetSalary;
                    worksheet.Cell(row, 16).Value = payroll.Status.ToString();

                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
