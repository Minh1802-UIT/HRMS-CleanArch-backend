using Employee.API.Common;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Attendance
{
    public static class WfhHandlers
    {
        // POST /api/attendance/wfh-approvals — Admin/HR grants WFH
        public static async Task<IResult> CreateWfhApproval(
            [FromBody] CreateWfhDto dto,
            IWfhApprovalRepository repo,
            ICurrentUser currentUser)
        {
            var approval = new WfhApproval(
                dto.EmployeeId, dto.FromDate, dto.ToDate,
                currentUser.UserId, dto.Reason);
            await repo.CreateAsync(approval);
            return ResultUtils.Success<object>(new { approval.Id }, "WFH approval created.");
        }

        // GET /api/attendance/wfh-approvals — list all active
        public static async Task<IResult> GetAllWfhApprovals(
            IWfhApprovalRepository repo)
        {
            var list = await repo.GetAllActiveAsync();
            return ResultUtils.Success<object>(list.Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.FromDate,
                a.ToDate,
                a.Reason,
                a.ApprovedBy,
                a.IsActive,
                a.CreatedAt
            }).ToList());
        }

        // GET /api/attendance/wfh-approvals/employee/{employeeId}
        public static async Task<IResult> GetByEmployee(
            string employeeId,
            IWfhApprovalRepository repo)
        {
            var list = await repo.GetByEmployeeAsync(employeeId);
            return ResultUtils.Success<object>(list.Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.FromDate,
                a.ToDate,
                a.Reason,
                a.ApprovedBy,
                a.IsActive,
                a.CreatedAt
            }).ToList());
        }

        // DELETE /api/attendance/wfh-approvals/{id} — revoke
        public static async Task<IResult> RevokeWfhApproval(
            string id,
            IWfhApprovalRepository repo)
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing == null) return ResultUtils.Fail("WFH_NOT_FOUND", "WFH approval not found.");

            existing.IsActive = false;
            existing.SetUpdatedAt(DateTime.UtcNow);
            await repo.UpdateAsync(id, existing);
            return ResultUtils.Success("WFH approval revoked.");
        }
    }

    public class CreateWfhDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Reason { get; set; }
    }
}
