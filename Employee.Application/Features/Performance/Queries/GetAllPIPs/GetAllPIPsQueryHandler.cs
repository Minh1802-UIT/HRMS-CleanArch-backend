using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Performance.Dtos;
using Employee.Application.Features.Performance.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Queries.GetAllPIPs
{
  public class GetAllPIPsQueryHandler : IRequestHandler<GetAllPIPsQuery, List<PIPResponseDto>>
  {
    private readonly IPIPRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetAllPIPsQueryHandler(IPIPRepository repo, IEmployeeRepository employeeRepo)
    {
      _repo = repo;
      _employeeRepo = employeeRepo;
    }

    public async Task<List<PIPResponseDto>> Handle(GetAllPIPsQuery request, CancellationToken cancellationToken)
    {
      var pips = await _repo.GetAllActiveAsync(cancellationToken);
      var pipList = pips.ToList();

      if (!pipList.Any())
        return new List<PIPResponseDto>();

      // Bulk-load all employee and manager names
      var allIds = pipList
        .SelectMany(p => new[] { p.EmployeeId, p.ManagerId })
        .Distinct()
        .ToList();

      var names = await _employeeRepo.GetNamesByIdsAsync(allIds, cancellationToken);

      var result = pipList.Select(pip =>
      {
        var empName = names.TryGetValue(pip.EmployeeId, out var e) ? e.Name : "";
        var mgrName = names.TryGetValue(pip.ManagerId, out var m) ? m.Name : "";
        return pip.ToDto(empName, mgrName);
      }).ToList();

      // Auto-expire overdue PIPs
      foreach (var pip in pipList.Where(p => p.Status == Domain.Enums.PIPStatus.InProgress && p.EndDate.Date < DateTime.UtcNow.Date))
      {
        pip.MarkExpiredIfPastDue();
        await _repo.UpdateAsync(pip.Id, pip, cancellationToken);
      }

      return result;
    }
  }
}
