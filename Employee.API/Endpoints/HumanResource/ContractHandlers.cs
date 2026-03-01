using Employee.API.Common; // ResultUtils
using Employee.Domain.Constants; // ErrorCodes
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Application.Common.Interfaces.Organization.IService; // IContractService
using Employee.Application.Common.Interfaces;
using Employee.Domain.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Employee.API.Endpoints.HumanResource
{
  public static class ContractHandlers
  {
    // 1. GET PAGED
    public static async Task<IResult> GetPaged(
        [AsParameters] PaginationParams pagination,
        IContractService service)
    {
      var result = await service.GetPagedAsync(pagination);
      return ResultUtils.Success(result, "Retrieved contract list successfully.");
    }

    // 2. GET BY ID
    public static async Task<IResult> GetById(string id, IContractService service)
    {
      var contract = await service.GetByIdAsync(id);
      return ResultUtils.Success(contract);
    }

    // 3. CREATE
    public static async Task<IResult> Create([FromBody] CreateContractDto dto, IContractService service)
    {
      // ?? GHI CHÚ QUAN TR?NG V? LOGIC BA:
      // Handler KHÔNG check logic "Kho?ng tr?ng h?p d?ng" hay "Ngày hi?u l?c".
      // Vi?c dó Service s? làm. N?u sai logic, Service ném Exception, Middleware s? b?t.

      var id = await service.CreateAsync(dto);

      // Tr? v? 201 Created
      return ResultUtils.Created(id, "Contract created successfully.");
    }

    // 4. UPDATE
    public static async Task<IResult> Update(string id, [FromBody] UpdateContractDto dto, IContractService service)
    {
      // 1. Validate ID kh?p (Filter không làm du?c vi?c này vì ID n?m trên URL)
      if (id != dto.Id)
      {
        return ResultUtils.Fail(ErrorCodes.InvalidData, "DevLog: URL ID does not match Body ID.");
      }

      // 2. G?i Service
      await service.UpdateAsync(id, dto);

      return ResultUtils.Success("Contract updated successfully.");
    }

    // 5. DELETE
    public static async Task<IResult> Delete(string id, IContractService service)
    {
      await service.DeleteAsync(id);
      return ResultUtils.Success("Contract deleted successfully.");
    }

    // 6. GET BY EMPLOYEE (Optimized)
    public static async Task<IResult> GetByEmployee(
        string employeeId,
        IContractService service,
        ICurrentUser currentUser,
        ILoggerFactory loggerFactory)
    {
      var logger = loggerFactory.CreateLogger("ContractHandlers");
      logger.LogDebug("Getting contracts for EmployeeID: {EmployeeId}", employeeId);
      try
      {
        var allowedRoles = new[] { "Admin", "HR", "Manager" };
        var hasRole = allowedRoles.Any(role => currentUser.IsInRole(role));
        var isOwner = currentUser.EmployeeId == employeeId;

        if (!hasRole && !isOwner)
        {
          logger.LogWarning("Access Denied. User {UserId} (Emp: {EmpId}) tried to access contracts of {TargetId}",
              currentUser.UserId, currentUser.EmployeeId, employeeId);
          return ResultUtils.Fail(ErrorCodes.Forbidden, "You do not have permission to view these contracts.");
        }

        var result = await service.GetByEmployeeIdAsync(employeeId);
        var list = result.ToList();
        logger.LogDebug("Found {Count} contracts for {EmployeeId}", list.Count, employeeId);

        return ResultUtils.Success(list, "Retrieved employee contracts successfully.");
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error retrieving contracts for {EmployeeId}", employeeId);
        return ResultUtils.Fail(ErrorCodes.InternalError, "An internal error occurred while retrieving contracts.");
      }
    }

    // 7. GET MY CONTRACTS (Self-service)
    public static async Task<IResult> GetMyContracts(
        IContractService service,
        ICurrentUser currentUser)
    {
      if (string.IsNullOrEmpty(currentUser.EmployeeId))
      {
        return ResultUtils.Fail("AUTH_UNLINKED", "Tài kho?n chua liên k?t nhân viên.");
      }

      var result = await service.GetByEmployeeIdAsync(currentUser.EmployeeId);
      return ResultUtils.Success(result, "Retrieved your contracts successfully.");
    }
  }
}
