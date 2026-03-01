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

    public UpdateEmployeeHandler(
        IEmployeeRepository repo,
        IDepartmentRepository deptRepo,
        IPositionRepository posRepo,
        IPublisher publisher)
    {
      _repo = repo;
      _deptRepo = deptRepo;
      _posRepo = posRepo;
      _publisher = publisher;
    }

    public async Task Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
      var oldEmp = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (oldEmp == null) throw new NotFoundException($"Không tìm thấy nhân viên có ID '{request.Id}'");

      // 0. Concurrency Check (Optimistic Locking)
      if (oldEmp.Version != request.Version)
      {
        throw new ConcurrencyException("Dữ liệu phiên bản cũ (Version mismatch). Vui lòng tải lại.");
      }

      // 1. Kiểm tra Phòng ban (nếu có thay đổi)
      if (oldEmp.JobDetails.DepartmentId != request.JobDetails.DepartmentId)
      {
        var dept = await _deptRepo.GetByIdAsync(request.JobDetails.DepartmentId, cancellationToken);
        if (dept == null) throw new NotFoundException($"Phòng ban có ID '{request.JobDetails.DepartmentId}' không tồn tại!");
      }

      // 2. Kiểm tra Chức vụ
      if (oldEmp.JobDetails.PositionId != request.JobDetails.PositionId)
      {
        var pos = await _posRepo.GetByIdAsync(request.JobDetails.PositionId, cancellationToken);
        if (pos == null) throw new NotFoundException($"Chức vụ có ID '{request.JobDetails.PositionId}' không tồn tại!");
      }

      var oldVal = new
      {
        Name = oldEmp.FullName,
        DeptId = oldEmp.JobDetails.DepartmentId,
        PosId = oldEmp.JobDetails.PositionId
      };

      // 3. Map từ Command sang Entity
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
    }
  }
}

