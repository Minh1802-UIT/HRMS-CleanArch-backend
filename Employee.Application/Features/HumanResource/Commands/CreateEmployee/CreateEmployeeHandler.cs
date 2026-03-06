using Employee.Application.Common;
using Employee.Application.Common.Models;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Application.Features.HumanResource.Mappers;
using Employee.Domain.Events;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.HumanResource.Commands.CreateEmployee
{
  public class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
  {
    private readonly IEmployeeRepository _repo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IPositionRepository _posRepo;
    private readonly IPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public CreateEmployeeHandler(
        IEmployeeRepository repo,
        IDepartmentRepository deptRepo,
        IPositionRepository posRepo,
        IPublisher publisher,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
      _repo = repo;
      _deptRepo = deptRepo;
      _posRepo = posRepo;
      _publisher = publisher;
      _unitOfWork = unitOfWork;
      _cache = cache;
    }

    public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
      await _unitOfWork.BeginTransactionAsync();
      try
      {
        // 1. Check for duplicate employee code
        var exists = await _repo.ExistsByCodeAsync(request.EmployeeCode, cancellationToken);
        if (exists)
          throw new ConflictException($"Employee code '{request.EmployeeCode}' already exists.");

        // 2. Validate department
        var dept = await _deptRepo.GetByIdAsync(request.JobDetails.DepartmentId, cancellationToken);
        if (dept == null)
          throw new NotFoundException($"Department with ID '{request.JobDetails.DepartmentId}' not found.");

        // 3. Validate position
        var pos = await _posRepo.GetByIdAsync(request.JobDetails.PositionId, cancellationToken);
        if (pos == null)
          throw new NotFoundException($"Position with ID '{request.JobDetails.PositionId}' not found.");

        // 4. Map command to entity
        var dto = new CreateEmployeeDto
        {
          EmployeeCode = request.EmployeeCode,
          FullName = request.FullName,
          Email = request.Email,
          AvatarUrl = request.AvatarUrl,
          PersonalInfo = request.PersonalInfo,
          JobDetails = request.JobDetails,
          BankDetails = request.BankDetails
        };

        var employee = dto.ToEntity();

        // 5. Persist to database
        await _repo.CreateAsync(employee, cancellationToken);

        // 6. Publish domain event
        var resultDto = employee.ToDto();
        await _publisher.Publish(
            new DomainEventNotification<EmployeeCreatedEvent>(
                new EmployeeCreatedEvent(employee.Id, employee.FullName, employee.Email, employee.PersonalInfo?.Phone ?? string.Empty)),
            cancellationToken);

        await _unitOfWork.CommitTransactionAsync();

        // 7. Invalidate caches
        await _cache.RemoveAsync(CacheKeys.EmployeeLookup);

        return resultDto;
      }
      catch (Exception)
      {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
      }
    }
  }
}

