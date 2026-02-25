using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Queries.GetPositionById
{
  public record GetPositionByIdQuery(string Id) : IRequest<PositionDto>;
}
