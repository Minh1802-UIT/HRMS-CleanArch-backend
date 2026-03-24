using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Performance.Dtos;
using Employee.Application.Features.Performance.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Queries.GetPIPById
{
  public class GetPIPByIdQueryHandler : IRequestHandler<GetPIPByIdQuery, PIPResponseDto?>
  {
    private readonly IPIPRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetPIPByIdQueryHandler(IPIPRepository repo, IEmployeeRepository employeeRepo)
    {
      _repo = repo;
      _employeeRepo = employeeRepo;
    }

    public async Task<PIPResponseDto?> Handle(GetPIPByIdQuery request, CancellationToken cancellationToken)
    {
      var pip = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (pip == null) return null;

      var employee = await _employeeRepo.GetByIdAsync(pip.EmployeeId, cancellationToken);
      var manager = await _employeeRepo.GetByIdAsync(pip.ManagerId, cancellationToken);

      return pip.ToDto(employee?.FullName ?? "Unknown", manager?.FullName ?? "Unknown");
    }
  }
}
