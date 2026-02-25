using Employee.Application.Common.Models;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeeLookup
{
  public class GetEmployeeLookupQueryHandler : IRequestHandler<GetEmployeeLookupQuery, List<LookupDto>>
  {
    private readonly IEmployeeRepository _repo;

    public GetEmployeeLookupQueryHandler(IEmployeeRepository repo)
    {
      _repo = repo;
    }

    public async Task<List<LookupDto>> Handle(GetEmployeeLookupQuery request, CancellationToken cancellationToken)
    {
      return await _repo.GetLookupAsync(request.Keyword, limit: 20, cancellationToken: cancellationToken);
    }
  }
}
