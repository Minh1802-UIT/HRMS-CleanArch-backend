using MongoDB.Driver;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Entities.Common;
using Employee.Infrastructure.Persistence;

namespace Employee.Infrastructure.Data
{
    public static class MongoIndexInitializer
    {
        public static async Task CreateIndexesAsync(IMongoContext context)
        {
            // 1. Employees
            var employees = context.GetCollection<EmployeeEntity>("employees");
            await employees.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<EmployeeEntity>(
                    Builders<EmployeeEntity>.IndexKeys.Ascending(x => x.IsDeleted).Ascending("JobDetails.DepartmentId"),
                    new CreateIndexOptions { Background = true }),
                new CreateIndexModel<EmployeeEntity>(
                    Builders<EmployeeEntity>.IndexKeys.Ascending(x => x.IsDeleted).Ascending("JobDetails.ManagerId"),
                    new CreateIndexOptions { Background = true }),
                new CreateIndexModel<EmployeeEntity>(
                    Builders<EmployeeEntity>.IndexKeys.Ascending(x => x.EmployeeCode),
                    new CreateIndexOptions { Unique = true, Background = true })
            });

            // 2. AttendanceBuckets
            var attendance = context.GetCollection<AttendanceBucket>("attendance_buckets");
            await attendance.Indexes.CreateOneAsync(
                new CreateIndexModel<AttendanceBucket>(
                    Builders<AttendanceBucket>.IndexKeys.Ascending(x => x.EmployeeId).Ascending(x => x.Month),
                    new CreateIndexOptions { Unique = true, Background = true }));

            // 3. Payrolls
            var payrolls = context.GetCollection<PayrollEntity>("payrolls");
            await payrolls.Indexes.CreateOneAsync(
                new CreateIndexModel<PayrollEntity>(
                    Builders<PayrollEntity>.IndexKeys.Ascending(x => x.EmployeeId).Ascending(x => x.Month),
                    new CreateIndexOptions { Unique = true, Background = true }));

            // 4. LeaveRequests
            var leaveRequests = context.GetCollection<LeaveRequest>("leave_requests");
            await leaveRequests.Indexes.CreateOneAsync(
                new CreateIndexModel<LeaveRequest>(
                    Builders<LeaveRequest>.IndexKeys.Ascending(x => x.EmployeeId).Ascending(x => x.IsDeleted).Ascending(x => x.Status),
                    new CreateIndexOptions { Background = true }));

            // 5. LeaveAllocations
            var leaveAllocations = context.GetCollection<LeaveAllocation>("leave_allocations");
            await leaveAllocations.Indexes.CreateOneAsync(
                new CreateIndexModel<LeaveAllocation>(
                    Builders<LeaveAllocation>.IndexKeys.Ascending(x => x.EmployeeId).Ascending(x => x.LeaveTypeId).Ascending(x => x.Year),
                    new CreateIndexOptions { Background = true }));

            // 6. Contracts
            var contracts = context.GetCollection<ContractEntity>("contracts");
            await contracts.Indexes.CreateOneAsync(
                new CreateIndexModel<ContractEntity>(
                    Builders<ContractEntity>.IndexKeys.Ascending(x => x.EmployeeId).Ascending(x => x.IsDeleted).Ascending(x => x.Status),
                    new CreateIndexOptions { Background = true }));

            // 7. SystemSettings
            var settings = context.GetCollection<SystemSetting>("system_settings");
            await settings.Indexes.CreateOneAsync(
                new CreateIndexModel<SystemSetting>(
                    Builders<SystemSetting>.IndexKeys.Ascending(x => x.Key),
                    new CreateIndexOptions { Unique = true, Background = true }));

            // 8. AuditLogs
            var auditLogs = context.GetCollection<AuditLog>("audit_logs");
            await auditLogs.Indexes.CreateOneAsync(
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Descending(x => x.CreatedAt),
                    new CreateIndexOptions { Background = true }));

            // 9. Candidates
            var candidates = context.GetCollection<Candidate>("candidates");
            await candidates.Indexes.CreateOneAsync(
                new CreateIndexModel<Candidate>(
                    Builders<Candidate>.IndexKeys.Ascending(x => x.JobVacancyId),
                    new CreateIndexOptions { Background = true }));

            // 10. RawAttendanceLogs (moved from repository constructor)
            var rawLogs = context.GetCollection<RawAttendanceLog>("raw_attendance_logs");
            await rawLogs.Indexes.CreateOneAsync(
                new CreateIndexModel<RawAttendanceLog>(
                    Builders<RawAttendanceLog>.IndexKeys.Ascending(x => x.IsProcessed).Ascending(x => x.Timestamp),
                    new CreateIndexOptions { Background = true }));
        }
    }
}
