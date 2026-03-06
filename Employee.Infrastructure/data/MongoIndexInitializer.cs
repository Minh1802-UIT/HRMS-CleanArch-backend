using MongoDB.Driver;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Entities.Common;
using Employee.Domain.Entities.Organization;
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
                // Supports default FullName sort in GetPagedListAsync and
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
                // Supports default StartDate DESC sort in ContractRepository.GetPagedAsync
                new CreateIndexModel<ContractEntity>(
                    Builders<ContractEntity>.IndexKeys
                        .Ascending(x => x.IsDeleted)
                        .Descending(x => x.StartDate),
                    new CreateIndexOptions { Background = true, Name = "idx_contracts_isDeleted_startDate_desc" }),
                // ContractExpirationBackgroundService queries by Status + EndDate
                new CreateIndexModel<ContractEntity>(
                    Builders<ContractEntity>.IndexKeys
                        .Ascending(x => x.Status)
                        .Ascending(x => x.EndDate),
                    new CreateIndexOptions { Background = true, Name = "idx_contracts_status_endDate" })
            });

            // 7. SystemSettings
            var settings = context.GetCollection<SystemSetting>("system_settings");
            await settings.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<SystemSetting>(
                    Builders<SystemSetting>.IndexKeys.Ascending(x => x.Key),
                    new CreateIndexOptions { Unique = true, Background = true }),
                // GetByGroupAsync filters by Group
                new CreateIndexModel<SystemSetting>(
                    Builders<SystemSetting>.IndexKeys.Ascending(x => x.Group),
                    new CreateIndexOptions { Background = true, Name = "idx_systemsettings_group" })
            });

            // 7b. Departments
            var departments = context.GetCollection<Department>("departments");
            await departments.Indexes.CreateManyAsync(new[]
            {
                // Filters for sub-department tree queries (GetChildrenAsync)
                new CreateIndexModel<Department>(
                    Builders<Department>.IndexKeys.Ascending(x => x.ParentId),
                    new CreateIndexOptions { Background = true, Name = "idx_departments_parentId" }),
                // Filters for GetByManagerIdAsync
                new CreateIndexModel<Department>(
                    Builders<Department>.IndexKeys.Ascending(x => x.ManagerId),
                    new CreateIndexOptions { Background = true, Name = "idx_departments_managerId" })
            });

            // 7c. Positions
            var positions = context.GetCollection<Position>("positions");
            await positions.Indexes.CreateManyAsync(new[]
            {
                // Supports GetByParentIdAsync (org chart traversal)
                new CreateIndexModel<Position>(
                    Builders<Position>.IndexKeys.Ascending(x => x.ParentId),
                    new CreateIndexOptions { Background = true, Name = "idx_positions_parentId" })
            });

            // 7d. Shifts
            var shifts = context.GetCollection<Shift>("shifts");
            await shifts.Indexes.CreateOneAsync(
                new CreateIndexModel<Shift>(
                    Builders<Shift>.IndexKeys.Ascending(x => x.Code),
                    new CreateIndexOptions { Unique = true, Background = true, Name = "idx_shifts_code" }));

            // 7e. LeaveTypes
            var leaveTypes = context.GetCollection<LeaveType>("leave_types");
            await leaveTypes.Indexes.CreateOneAsync(
                new CreateIndexModel<LeaveType>(
                    Builders<LeaveType>.IndexKeys.Ascending(x => x.Code),
                    new CreateIndexOptions { Unique = true, Background = true, Name = "idx_leavetypes_code" }));

            // 8. AuditLogs
            var auditLogs = context.GetCollection<AuditLog>("audit_logs");
            await auditLogs.Indexes.CreateManyAsync(new[]
            {
                // Existing: base sort for the offset-based paged query
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Descending(x => x.CreatedAt),
                    new CreateIndexOptions { Background = true }),
                // Cursor-based sort (CreatedAt DESC, _id DESC) for GetLogsCursorPagedAsync
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys
                        .Descending(x => x.CreatedAt)
                        .Descending("_id"),
                    new CreateIndexOptions { Background = true, Name = "idx_auditlogs_createdAt_id_desc" }),
                // Filter by UserId + sort — avoids full-collection scan when userId is supplied
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys
                        .Ascending(x => x.UserId)
                        .Descending(x => x.CreatedAt),
                    new CreateIndexOptions { Background = true, Name = "idx_auditlogs_userId_createdAt" }),
                // Filter by Action + sort
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys
                        .Ascending(x => x.Action)
                        .Descending(x => x.CreatedAt),
                    new CreateIndexOptions { Background = true, Name = "idx_auditlogs_action_createdAt" }),
                // Text index enables $text search on UserName / TableName / Action.
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
            await rawLogs.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<RawAttendanceLog>(
                    Builders<RawAttendanceLog>.IndexKeys.Ascending(x => x.IsProcessed).Ascending(x => x.Timestamp),
                    new CreateIndexOptions { Background = true }),
                // GetByEmployeeIdAsync filters by EmployeeId + Timestamp
                new CreateIndexModel<RawAttendanceLog>(
                    Builders<RawAttendanceLog>.IndexKeys
                        .Ascending(x => x.EmployeeId)
                        .Ascending(x => x.Timestamp),
                    new CreateIndexOptions { Background = true, Name = "idx_rawattendancelogs_employeeId_timestamp" })
            });

      // 11. PerformanceReviews — supports GetByEmployeeIdAsync
      var perfReviews = context.GetCollection<Employee.Domain.Entities.Performance.PerformanceReview>("performance_reviews");
      await perfReviews.Indexes.CreateOneAsync(
          new CreateIndexModel<Employee.Domain.Entities.Performance.PerformanceReview>(
              Builders<Employee.Domain.Entities.Performance.PerformanceReview>.IndexKeys
                  .Ascending(x => x.EmployeeId).Ascending(x => x.IsDeleted),
              new CreateIndexOptions { Background = true, Name = "idx_perfreviews_employeeId_isDeleted" }));

      // 12. PerformanceGoals — supports GetByEmployeeIdAsync
      var perfGoals = context.GetCollection<Employee.Domain.Entities.Performance.PerformanceGoal>("performance_goals");
      await perfGoals.Indexes.CreateOneAsync(
          new CreateIndexModel<Employee.Domain.Entities.Performance.PerformanceGoal>(
              Builders<Employee.Domain.Entities.Performance.PerformanceGoal>.IndexKeys
                  .Ascending(x => x.EmployeeId).Ascending(x => x.IsDeleted),
              new CreateIndexOptions { Background = true, Name = "idx_perfgoals_employeeId_isDeleted" }));

      // 13. JobVacancies — supports GetAllAsync, CountActiveAsync
      var jobVacancies = context.GetCollection<JobVacancy>("job_vacancies");
      await jobVacancies.Indexes.CreateOneAsync(
          new CreateIndexModel<JobVacancy>(
              Builders<JobVacancy>.IndexKeys.Ascending(x => x.IsDeleted),
              new CreateIndexOptions { Background = true, Name = "idx_jobvacancies_isDeleted" }));

      // 14. Payrolls — supports GetByMonthAsync, FinalizePayrollAsync
      await payrolls.Indexes.CreateOneAsync(
          new CreateIndexModel<PayrollEntity>(
              Builders<PayrollEntity>.IndexKeys.Ascending(x => x.Month).Ascending(x => x.Status),
              new CreateIndexOptions { Background = true, Name = "idx_payrolls_month_status" }));

      // 15. LeaveRequests — supports default sort by FromDate DESC in GetPagedAsync
      await leaveRequests.Indexes.CreateOneAsync(
          new CreateIndexModel<LeaveRequest>(
              Builders<LeaveRequest>.IndexKeys
                  .Ascending(x => x.IsDeleted).Descending(x => x.FromDate),
              new CreateIndexOptions { Background = true, Name = "idx_leaverequests_isDeleted_fromDate_desc" }));

      // 16. Notifications — supports GetByUserIdAsync, GetUnreadCountAsync
      var notifications = context.GetCollection<Employee.Domain.Entities.Notifications.Notification>("notifications");
      await notifications.Indexes.CreateOneAsync(
          new CreateIndexModel<Employee.Domain.Entities.Notifications.Notification>(
              Builders<Employee.Domain.Entities.Notifications.Notification>.IndexKeys
                  .Ascending(x => x.UserId).Ascending(x => x.IsRead).Descending(x => x.CreatedAt),
              new CreateIndexOptions { Background = true, Name = "idx_notifications_userId_isRead_createdAt" }));

            // 18. OvertimeSchedules — unique per employee per date
            var otSchedules = context.GetCollection<OvertimeSchedule>("overtime_schedules");
            await otSchedules.Indexes.CreateManyAsync(new[]
            {
          new CreateIndexModel<OvertimeSchedule>(
              Builders<OvertimeSchedule>.IndexKeys
                  .Ascending(x => x.EmployeeId).Ascending(x => x.Date),
              new CreateIndexOptions { Unique = true, Background = true, Name = "idx_otschedules_employeeId_date" }),
          new CreateIndexModel<OvertimeSchedule>(
              Builders<OvertimeSchedule>.IndexKeys.Ascending(x => x.Date),
              new CreateIndexOptions { Background = true, Name = "idx_otschedules_date" })
            });

            // 17. AttendanceExplanations — supports GetByEmployeeIdAsync, GetPendingAsync, GetByEmployeeAndDateAsync
            var explanations = context.GetCollection<AttendanceExplanation>("attendance_explanations");
            await explanations.Indexes.CreateManyAsync(new[]
            {
          new CreateIndexModel<AttendanceExplanation>(
              Builders<AttendanceExplanation>.IndexKeys
                  .Ascending(x => x.EmployeeId).Descending(x => x.WorkDate),
              new CreateIndexOptions { Background = true, Name = "idx_explanations_employeeId_workDate" }),
          new CreateIndexModel<AttendanceExplanation>(
              Builders<AttendanceExplanation>.IndexKeys
                  .Ascending(x => x.Status).Ascending(x => x.CreatedAt),
              new CreateIndexOptions { Background = true, Name = "idx_explanations_status_createdAt" }),
          // unique: one explanation per employee per work-date
          new CreateIndexModel<AttendanceExplanation>(
              Builders<AttendanceExplanation>.IndexKeys
                  .Ascending(x => x.EmployeeId).Ascending(x => x.WorkDate),
              new CreateIndexOptions { Background = true, Unique = true, Name = "idx_explanations_employeeId_workDate_unique" })
      });
        }
    }
}
