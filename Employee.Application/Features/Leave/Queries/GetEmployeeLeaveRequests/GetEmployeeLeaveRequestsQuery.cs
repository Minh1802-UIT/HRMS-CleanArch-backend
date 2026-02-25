using Employee.Application.Features.Leave.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Leave.Queries.GetEmployeeLeaveRequests
{
  public record GetEmployeeLeaveRequestsQuery(string EmployeeId) : IRequest<IEnumerable<LeaveRequestDto>>;
}
