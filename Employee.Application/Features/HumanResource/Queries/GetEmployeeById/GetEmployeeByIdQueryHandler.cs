using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Application.Features.HumanResource.Mappers;
using MediatR;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeeById
{
  public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
  {
    private readonly IEmployeeRepository _repo;
    private readonly ICurrentUser _currentUser;

    public GetEmployeeByIdQueryHandler(IEmployeeRepository repo, ICurrentUser currentUser)
    {
      _repo = repo;
      _currentUser = currentUser;
    }

    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (entity == null) throw new NotFoundException($"Employee with ID {request.Id} not found.");

      var dto = entity.ToDto();

      var isOwner = _currentUser.EmployeeId == request.Id;
      var isAdminOrHR = _currentUser.IsInRole("Admin") || _currentUser.IsInRole("HR");

      if (!isOwner && !isAdminOrHR)
      {
        dto.BankDetails = null;
      }

      return dto;
    }
  }
}
