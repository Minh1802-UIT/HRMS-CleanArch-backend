using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using MediatR;
using Employee.Application.Common.Behaviors;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Attendance.IService;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Services;
using Employee.Application.Common.Services.DashboardProviders;
using Employee.Application.Features.Attendance.Logic;
using Employee.Application.Features.Attendance.Services;
using Employee.Application.Features.HumanResource.Services;
using Employee.Application.Features.Leave.Services;
using Employee.Application.Features.Payroll.Services;
using Employee.Application.Features.Notifications.Services;
using Employee.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace Employee.Application
{
  public static class DependencyInjection
  {
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
      var assembly = Assembly.GetExecutingAssembly();

      // Register MediatR
      services.AddMediatR(cfg =>
      {
        cfg.RegisterServicesFromAssembly(assembly);
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
      });

      // Register FluentValidation
      services.AddValidatorsFromAssembly(assembly);

      // ==========================================
      // 1. SYSTEM & DASHBOARD SERVICES
      // ==========================================
      services.AddScoped<IAuditLogService, AuditLogService>();
      services.AddScoped<IDashboardService, DashboardService>();
      services.AddScoped<IDashboardProvider, HrDashboardProvider>();
      services.AddScoped<IDashboardProvider, LeaveDashboardProvider>();
      services.AddScoped<IDashboardProvider, RecruitmentDashboardProvider>();
      services.AddScoped<ISystemSettingService, SystemSettingService>();

      // ==========================================
      // 2. CORE BUSINESS SERVICES
      // ==========================================
      services.AddScoped<IContractService, ContractService>();

      // Attendance
      var timezoneOffset = configuration.GetValue<int>("SystemSettings:TimezoneOffsetHours", 7);
      services.AddScoped<AttendanceCalculator>(sp => new AttendanceCalculator(TimeSpan.FromHours(timezoneOffset)));
      services.AddScoped<IShiftService, ShiftService>();
      services.AddScoped<IAttendanceService, AttendanceService>();
      services.AddScoped<IAttendanceProcessingService, AttendanceProcessingService>();

      // Leave
      services.AddScoped<ILeaveTypeService, LeaveTypeService>();
      services.AddScoped<ILeaveAllocationService, LeaveAllocationService>();

      // Notifications (NEW-9)
      services.AddScoped<INotificationService, NotificationService>();

      // Payroll
      services.AddScoped<IPayrollService, PayrollService>();
      services.AddScoped<IPayrollProcessingService, PayrollProcessingService>();
      services.AddScoped<IPayrollDataProvider, PayrollDataProvider>();
      services.AddScoped<IPayslipService, PayslipService>();
      services.AddScoped<IExcelExportService, ExcelExportService>();
      services.AddScoped<Employee.Domain.Services.Payroll.ITaxCalculator, Employee.Domain.Services.Payroll.VietnameseTaxCalculator>();

      return services;
    }
  }
}
