using Employee.Application.Features.Performance.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Performance.Queries.GetPIPById
{
  public record GetPIPByIdQuery(string Id) : IRequest<PIPResponseDto?>;
}
