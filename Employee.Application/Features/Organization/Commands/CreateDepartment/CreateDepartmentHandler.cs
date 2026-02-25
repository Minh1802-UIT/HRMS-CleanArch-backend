using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Commands.CreateDepartment
{
  public class CreateDepartmentHandler(IDepartmentRepository repo, ICacheService cache) : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
  {
    private const string DEPARTMENT_TREE_KEY = "DEPARTMENT_TREE";

    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
      if (!string.IsNullOrEmpty(request.Dto.ParentId))
      {
        var parent = await repo.GetByIdAsync(request.Dto.ParentId, cancellationToken);
        if (parent == null) throw new NotFoundException($"Parent department {request.Dto.ParentId} not found");
      }

      var dept = request.Dto.ToEntity();
      await repo.CreateAsync(dept, cancellationToken);
      await cache.RemoveAsync(DEPARTMENT_TREE_KEY);
      return dept.ToDto()!;
    }
  }
}
