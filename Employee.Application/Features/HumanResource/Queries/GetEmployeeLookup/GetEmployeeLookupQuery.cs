using Employee.Domain.Common.Models;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeeLookup
{
  public record GetEmployeeLookupQuery(string? Keyword) : IRequest<List<LookupDto>>;
}
