using MediatR;
using System.Collections.Generic;
using Employee.Application.Features.Performance.Dtos;

namespace Employee.Application.Features.Performance.Queries.GetAllPIPs
{
  /// <summary>
  /// Gets all active PIPs (not Completed or Cancelled).
  /// </summary>
  public record GetAllPIPsQuery : IRequest<List<PIPResponseDto>>;
}
