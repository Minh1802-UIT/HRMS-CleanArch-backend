using Employee.Application.Common;
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
    private readonly ICacheService _cache;

    public GetEmployeeByIdQueryHandler(IEmployeeRepository repo, ICurrentUser currentUser, ICacheService cache)
    {
      _repo = repo;
      _currentUser = currentUser;
      _cache = cache;
    }

    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
      // Check cache first (stores full DTO — BankDetails masked below if needed)
      var cacheKey = CacheKeys.Employee(request.Id);
      var cachedDto = await _cache.GetAsync<EmployeeDto>(cacheKey);

      if (cachedDto == null)
      {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null) throw new NotFoundException($"Employee with ID {request.Id} not found.");

        cachedDto = entity.ToDto();
        await _cache.SetAsync(cacheKey, cachedDto, TimeSpan.FromMinutes(10));
      }

      // Mask sensitive info per caller role
      var isOwner = _currentUser.EmployeeId == request.Id;
      var isAdminOrHR = _currentUser.IsInRole("Admin") || _currentUser.IsInRole("HR");

      if (!isOwner && !isAdminOrHR)
      {
        cachedDto.BankDetails = null;
      }

      return cachedDto;
    }
  }
}
