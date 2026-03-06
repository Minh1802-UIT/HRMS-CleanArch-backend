using Employee.Application.Common;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Models;
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

namespace Employee.Application.Features.HumanResource.Commands.UpdateEmployee
{
  public class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand>
  {
    private readonly IEmployeeRepository _repo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IPositionRepository _posRepo;
    private readonly IPublisher _publisher;
    private readonly ICacheService _cache;

    public UpdateEmployeeHandler(
        IEmployeeRepository repo,
        IDepartmentRepository deptRepo,
        IPositionRepository posRepo,
        IPublisher publisher,
        ICacheService cache)
    {
      _repo = repo;
      _deptRepo = deptRepo;
      _posRepo = posRepo;
      _publisher = publisher;
      _cache = cache;
    }

    public async Task Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
      var oldEmp = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (oldEmp == null) throw new NotFoundException($"Employee with ID '{request.Id}' not found.");

      // 0. Concurrency check (optimistic locking)
      if (oldEmp.Version != request.Version)
      {
        throw new ConcurrencyException("Data has been modified by another request (version mismatch). Please reload and try again.");
      }

      // 1. Validate department (if changed)
      if (oldEmp.JobDetails.DepartmentId != request.JobDetails.DepartmentId)
      {
        var dept = await _deptRepo.GetByIdAsync(request.JobDetails.DepartmentId, cancellationToken);
        if (dept == null) throw new NotFoundException($"Department with ID '{request.JobDetails.DepartmentId}' not found.");
      }

      // 2. Validate position (if changed)
      if (oldEmp.JobDetails.PositionId != request.JobDetails.PositionId)
      {
        var pos = await _posRepo.GetByIdAsync(request.JobDetails.PositionId, cancellationToken);
        if (pos == null) throw new NotFoundException($"Position with ID '{request.JobDetails.PositionId}' not found.");
      }

      var oldVal = new
      {
        Name = oldEmp.FullName,
        DeptId = oldEmp.JobDetails.DepartmentId,
        PosId = oldEmp.JobDetails.PositionId
      };

      // 3. Map command to entity
      var dto = new UpdateEmployeeDto
      {
        Id = request.Id,
        FullName = request.FullName,
        Email = request.Email,
        AvatarUrl = request.AvatarUrl,
        PersonalInfo = request.PersonalInfo,
        JobDetails = request.JobDetails,
        BankDetails = request.BankDetails
      };

      oldEmp.UpdateFromDto(dto);

      // 4. Lưu
      await _repo.UpdateAsync(request.Id, oldEmp, request.Version, cancellationToken);

      // 5. 📢 Bắn sự kiện "Nhân viên đã cập nhật" để ghi Log
      await _publisher.Publish(
          new DomainEventNotification<EmployeeUpdatedEvent>(
              new EmployeeUpdatedEvent(
                  request.Id,
                  System.Text.Json.JsonSerializer.Serialize(oldVal),
                  System.Text.Json.JsonSerializer.Serialize(new { dto.FullName, dto.Email, dto.JobDetails?.DepartmentId, dto.JobDetails?.PositionId }))),
          cancellationToken);

      // 6. Invalidate caches
      await _cache.RemoveAsync(CacheKeys.Employee(request.Id));
      await _cache.RemoveAsync(CacheKeys.EmployeeLookup);
    }
  }
}

