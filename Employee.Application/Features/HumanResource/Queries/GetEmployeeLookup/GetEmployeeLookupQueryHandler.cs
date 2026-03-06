using Employee.Application.Common;
using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeeLookup
{
  public class GetEmployeeLookupQueryHandler : IRequestHandler<GetEmployeeLookupQuery, List<LookupDto>>
  {
    private readonly IEmployeeQueryRepository _repo;
    private readonly ICacheService _cache;

    public GetEmployeeLookupQueryHandler(IEmployeeQueryRepository repo, ICacheService cache)
    {
      _repo = repo;
      _cache = cache;
    }

    public async Task<List<LookupDto>> Handle(GetEmployeeLookupQuery request, CancellationToken cancellationToken)
    {
      // Only cache the unfiltered default lookup (no keyword, no department filter, default limit)
      var useCache = string.IsNullOrEmpty(request.Keyword) && request.Limit == 20 && string.IsNullOrEmpty(request.DepartmentId);

      if (useCache)
      {
        var cached = await _cache.GetAsync<List<LookupDto>>(CacheKeys.EmployeeLookup);
        if (cached != null) return cached;
      }

      var result = await _repo.GetLookupAsync(request.Keyword, limit: request.Limit, departmentId: request.DepartmentId, cancellationToken: cancellationToken);

      if (useCache)
      {
        await _cache.SetAsync(CacheKeys.EmployeeLookup, result, TimeSpan.FromMinutes(15));
      }

      return result;
    }
  }
}
