using Employee.Application.Features.Organization.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Organization.Queries.GetPositionTree
{
  public record GetPositionTreeQuery() : IRequest<List<PositionNodeDto>>;
}
