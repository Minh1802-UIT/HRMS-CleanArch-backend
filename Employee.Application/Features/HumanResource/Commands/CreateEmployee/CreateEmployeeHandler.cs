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

    public CreateEmployeeHandler(
        IEmployeeRepository repo,
        IDepartmentRepository deptRepo,
        IPositionRepository posRepo,
        IPublisher publisher,
        IUnitOfWork unitOfWork)
    {
      _repo = repo;
      _deptRepo = deptRepo;
      _posRepo = posRepo;
      _publisher = publisher;
      _unitOfWork = unitOfWork;
    }

    public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
      await _unitOfWork.BeginTransactionAsync();
      try
      {
        // 1. Kiểm tra trùng mã nhân viên
        var exists = await _repo.ExistsByCodeAsync(request.EmployeeCode, cancellationToken);
        if (exists)
          throw new ConflictException($"Mã nhân viên '{request.EmployeeCode}' đã tồn tại!");

        // 2. Kiểm tra Phòng ban
        var dept = await _deptRepo.GetByIdAsync(request.JobDetails.DepartmentId, cancellationToken);
        if (dept == null)
          throw new NotFoundException($"Phòng ban có ID '{request.JobDetails.DepartmentId}' không tồn tại!");

        // 3. Kiểm tra Chức vụ
        var pos = await _posRepo.GetByIdAsync(request.JobDetails.PositionId, cancellationToken);
        if (pos == null)
          throw new NotFoundException($"Chức vụ có ID '{request.JobDetails.PositionId}' không tồn tại!");

        // 4. Map từ Command sang Entity
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

        // 5. Lưu vào DB
        await _repo.CreateAsync(employee, cancellationToken);

        // 6. 📢 Bắn sự kiện "Nhân viên đã được tạo"
        var resultDto = employee.ToDto();
        await _publisher.Publish(
            new DomainEventNotification<EmployeeCreatedEvent>(
                new EmployeeCreatedEvent(employee.Id, employee.FullName, employee.Email, employee.PersonalInfo?.Phone ?? string.Empty)),
            cancellationToken);

        await _unitOfWork.CommitTransactionAsync();
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

