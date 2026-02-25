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
                    new CreateIndexOptions { Unique = true, Background = true }),
                // NEW: supports default FullName sort in GetPagedListAsync and
                //      reduces the scan set for GetLookupAsync regex searches.
                new CreateIndexModel<EmployeeEntity>(
                    Builders<EmployeeEntity>.IndexKeys
                        .Ascending(x => x.IsDeleted)
                        .Ascending(x => x.FullName),
                    new CreateIndexOptions { Background = true, Name = "idx_employees_isDeleted_fullName" }),
                // Text index for full-text search on name / code via $text queries.
                // Note: regex ($regex) queries do NOT use this index; convert to
                //       $text or use anchored prefix regex to benefit from indexes.
                new CreateIndexModel<EmployeeEntity>(
                    Builders<EmployeeEntity>.IndexKeys
                        .Text(x => x.FullName)
                        .Text(x => x.EmployeeCode),
                    new CreateIndexOptions { Background = true, Name = "idx_employees_text" })
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
            await contracts.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<ContractEntity>(
                    Builders<ContractEntity>.IndexKeys.Ascending(x => x.EmployeeId).Ascending(x => x.IsDeleted).Ascending(x => x.Status),
                    new CreateIndexOptions { Background = true }),
                // NEW: supports default StartDate DESC sort in ContractRepository.GetPagedAsync
                new CreateIndexModel<ContractEntity>(
                    Builders<ContractEntity>.IndexKeys
                        .Ascending(x => x.IsDeleted)
                        .Descending(x => x.StartDate),
                    new CreateIndexOptions { Background = true, Name = "idx_contracts_isDeleted_startDate_desc" }),
                // NEW: ContractExpirationBackgroundService queries by Status + EndDate
                new CreateIndexModel<ContractEntity>(
                    Builders<ContractEntity>.IndexKeys
                        .Ascending(x => x.Status)
                        .Ascending(x => x.EndDate),
                    new CreateIndexOptions { Background = true, Name = "idx_contracts_status_endDate" })
            });

            // 7. SystemSettings
            var settings = context.GetCollection<SystemSetting>("system_settings");
            await settings.Indexes.CreateOneAsync(
                new CreateIndexModel<SystemSetting>(
                    Builders<SystemSetting>.IndexKeys.Ascending(x => x.Key),
                    new CreateIndexOptions { Unique = true, Background = true }));

            // 8. AuditLogs
            var auditLogs = context.GetCollection<AuditLog>("audit_logs");
            await auditLogs.Indexes.CreateManyAsync(new[]
            {
                // Existing: base sort for the offset-based paged query
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Descending(x => x.CreatedAt),
                    new CreateIndexOptions { Background = true }),
                // NEW: cursor-based sort (CreatedAt DESC, _id DESC) for GetLogsCursorPagedAsync
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys
                        .Descending(x => x.CreatedAt)
                        .Descending("_id"),
                    new CreateIndexOptions { Background = true, Name = "idx_auditlogs_createdAt_id_desc" }),
                // NEW: filter by UserId + sort — avoids full-collection scan when userId is supplied
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys
                        .Ascending(x => x.UserId)
                        .Descending(x => x.CreatedAt),
                    new CreateIndexOptions { Background = true, Name = "idx_auditlogs_userId_createdAt" }),
                // NEW: filter by Action + sort
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys
                        .Ascending(x => x.Action)
                        .Descending(x => x.CreatedAt),
                    new CreateIndexOptions { Background = true, Name = "idx_auditlogs_action_createdAt" }),
                // NEW: text index enables $text search on UserName / TableName / Action.
                // Tip: replace the current $regex filters with $text for O(log N) search.
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys
                        .Text(x => x.UserName)
                        .Text(x => x.TableName)
                        .Text(x => x.Action),
                    new CreateIndexOptions { Background = true, Name = "idx_auditlogs_text" })
            });

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
