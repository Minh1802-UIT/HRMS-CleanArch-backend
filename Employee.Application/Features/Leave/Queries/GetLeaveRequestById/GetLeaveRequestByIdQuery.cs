using Employee.Application.Features.Leave.Dtos;
using MediatR;

namespace Employee.Application.Features.Leave.Queries.GetLeaveRequestById
{
  public record GetLeaveRequestByIdQuery(string Id) : IRequest<LeaveRequestDto>;
}
