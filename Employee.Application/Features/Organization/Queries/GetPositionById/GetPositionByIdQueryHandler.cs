using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Queries.GetPositionById
{
  public class GetPositionByIdQueryHandler(IPositionRepository repo) : IRequestHandler<GetPositionByIdQuery, PositionDto>
  {
    public async Task<PositionDto> Handle(GetPositionByIdQuery request, CancellationToken cancellationToken)
    {
      var entity = await repo.GetByIdAsync(request.Id, cancellationToken);
      if (entity == null) throw new NotFoundException($"Position with ID {request.Id} not found.");

      var dto = entity.ToDto();
      if (!string.IsNullOrEmpty(entity.ParentId))
      {
        var parent = await repo.GetByIdAsync(entity.ParentId, cancellationToken);
        dto.ParentTitle = parent?.Title;
      }
      return dto;
    }
  }
}
