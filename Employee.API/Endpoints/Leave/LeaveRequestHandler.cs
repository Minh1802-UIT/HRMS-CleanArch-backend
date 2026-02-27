using Employee.API.Common;
using Employee.Domain.Constants;
using Employee.Application.Common.Models;
using Employee.Application.Features.Leave.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Employee.Application.Features.Leave.Commands.CreateLeaveRequest;
using Employee.Application.Features.Leave.Commands.ReviewLeaveRequest;
using Employee.Application.Features.Leave.Commands.CancelLeaveRequest;
using Employee.Application.Features.Leave.Commands.UpdateLeaveRequest;
using Employee.Application.Features.Leave.Queries.GetLeaveRequestsPaged;
using Employee.Application.Features.Leave.Queries.GetEmployeeLeaveRequests;
using Employee.Application.Features.Leave.Queries.GetLeaveRequestById;

namespace Employee.API.Endpoints.Leave
{
  public static class LeaveRequestHandlers
  {
    public static async Task<IResult> GetPagedList(
      [FromBody] PaginationParams pagination,
      ISender sender)
    {
      var result = await sender.Send(new GetLeaveRequestsPagedQuery(pagination));
      return ResultUtils.Success(result, "Retrieved paginated leave requests successfully.");
    }

    // 1. GET MY LEAVES (Xem lịch sử nghỉ phép của chính mình)
    public static async Task<IResult> GetMyLeaves(
        ISender sender,
        ICurrentUser currentUser)
    {
      if (string.IsNullOrEmpty(currentUser.EmployeeId))
      {
        return ResultUtils.Success(Array.Empty<LeaveRequestDto>(), "User is not linked to any employee record.");
      }
      var list = await sender.Send(new GetEmployeeLeaveRequestsQuery(currentUser.EmployeeId));
      return ResultUtils.Success(list);
    }

    // 2. GET BY ID (Xem chi tiết 1 đơn)
    public static async Task<IResult> GetById(string id, ISender sender, ICurrentUser currentUser)
    {
      var item = await sender.Send(new GetLeaveRequestByIdQuery(id));
      // Employee chỉ được xem đơn nghỉ phép của chính mình; Admin/HR/Manager được xem tất cả
      if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("HR") && !currentUser.IsInRole("Manager"))
      {
        var employeeId = currentUser.EmployeeId ?? currentUser.UserId;
        if (item.EmployeeId != employeeId)
          return ResultUtils.Fail("LEAVE_REQUEST_FORBIDDEN", "Bạn không có quyền xem đơn nghỉ phép này.", 403);
      }
      return ResultUtils.Success(item);
    }

    // 3. CREATE (Tạo đơn xin nghỉ mới - CQRS)
    public static async Task<IResult> Create(
        [FromBody] CreateLeaveRequestDto dto,
        ISender sender,
        ICurrentUser currentUser)
    {
      var command = new CreateLeaveRequestCommand
      {
        LeaveType = dto.LeaveType,
        FromDate = dto.FromDate,
        ToDate = dto.ToDate,
        Reason = dto.Reason,
        EmployeeId = currentUser.EmployeeId ?? currentUser.UserId
      };

      var resultDto = await sender.Send(command);
      return ResultUtils.Created(resultDto, "Leave request submitted successfully via CQRS.");
    }

    // 4. UPDATE (Sửa đơn - CQRS)
    public static async Task<IResult> Update(
        string id,
        [FromBody] UpdateLeaveRequestDto dto,
        ISender sender,
        ICurrentUser currentUser)
    {
      if (id != dto.Id)
      {
        return ResultUtils.Fail(ErrorCodes.InvalidData, "DevLog: URL ID mismatch.");
      }

      var command = new UpdateLeaveRequestCommand
      {
        Id = id,
        Dto = dto,
        EmployeeId = currentUser.EmployeeId ?? currentUser.UserId
      };

      await sender.Send(command);

      return ResultUtils.Success("Leave request updated successfully via CQRS.");
    }

    // 5. CANCEL (Hủy đơn - CQRS)
    public static async Task<IResult> Cancel(
        string id,
        ISender sender,
        ICurrentUser currentUser)
    {
      var command = new CancelLeaveRequestCommand(id, currentUser.EmployeeId ?? currentUser.UserId);
      await sender.Send(command);
      return ResultUtils.Success("Leave request cancelled successfully via CQRS.");
    }

    // 6. REVIEW (Sếp duyệt đơn - CQRS)
    public static async Task<IResult> Review(
        string id,
        [FromBody] ReviewLeaveRequestDto dto,
        ISender sender,
        ICurrentUser currentUser)
    {
      if (id != dto.Id)
      {
        return ResultUtils.Fail(ErrorCodes.InvalidData, "DevLog: URL ID mismatch.");
      }

      var command = new ReviewLeaveRequestCommand
      {
        Id = id,
        ReviewDto = dto,
        ApprovedBy = currentUser.EmployeeId ?? currentUser.UserId,
        ApprovedByName = currentUser.UserName ?? currentUser.UserId
      };

      await sender.Send(command);

      return ResultUtils.Success($"Leave request has been {dto.Status.ToLower()} via CQRS.");
    }
  }
}