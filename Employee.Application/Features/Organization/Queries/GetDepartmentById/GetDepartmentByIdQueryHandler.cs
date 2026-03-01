using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Queries.GetDepartmentById
{
  public class GetDepartmentByIdQueryHandler(IDepartmentRepository repo) : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto>
  {
    public async Task<DepartmentDto> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
      var dept = await repo.GetByIdAsync(request.Id, cancellationToken);
      if (dept == null) throw new NotFoundException($"Department with ID {request.Id} not found.");
      return dept.ToDto()!;
    }
  }
}
