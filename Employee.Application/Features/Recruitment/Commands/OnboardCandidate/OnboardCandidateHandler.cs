using Employee.Application.Common.Models;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Events;
using Employee.Application.Features.HumanResource.Mappers;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.OnboardCandidate
{
  public class OnboardCandidateHandler : IRequestHandler<OnboardCandidateCommand, string>
  {
    private readonly ICandidateRepository _candidateRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;

    public OnboardCandidateHandler(
        ICandidateRepository candidateRepo,
        IEmployeeRepository employeeRepo,
        IPublisher publisher,
        IUnitOfWork unitOfWork)
    {
      _candidateRepo = candidateRepo;
      _employeeRepo = employeeRepo;
      _publisher = publisher;
      _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(OnboardCandidateCommand request, CancellationToken cancellationToken)
    {
      var candidate = await _candidateRepo.GetByIdAsync(request.CandidateId, cancellationToken)
          ?? throw new ValidationException("Candidate not found.");

      if (candidate.Status != CandidateStatus.Hired)
        throw new ValidationException("Only candidates with 'Hired' status can be onboarded.");

      // 3. Check EmployeeCode duplicate (same rule as Direct Create)
      var codeExists = await _employeeRepo.ExistsByCodeAsync(request.OnboardData.EmployeeCode, cancellationToken);
      if (codeExists)
        throw new ValidationException($"Employee code '{request.OnboardData.EmployeeCode}' already exists.");

      await _unitOfWork.BeginTransactionAsync();
      try
      {
        // 1. Create Employee Entity
        var employee = new EmployeeEntity(
            request.OnboardData.EmployeeCode,
            candidate.FullName,
            candidate.Email
        );

        // 2. Set Job Details
        // Status defaults to Probation (same as Direct Create) — onboarded candidates
        // still go through a probation period before becoming fully Active.
        var job = new JobDetails
        {
          DepartmentId = request.OnboardData.DepartmentId,
          PositionId = request.OnboardData.PositionId,
          ManagerId = request.OnboardData.ManagerId ?? string.Empty,
          JoinDate = request.OnboardData.JoinDate,
          Status = EmployeeStatus.Probation
        };
        employee.UpdateJobDetails(job);

        // 3. Set Personal Info (Phone from Candidate, DOB from onboard request)
        var personalInfo = new PersonalInfo
        {
          Phone = candidate.Phone,
          Dob = request.OnboardData.DateOfBirth
        };
        employee.UpdatePersonalInfo(personalInfo);

        await _employeeRepo.CreateAsync(employee, cancellationToken);

        // 4. Update Candidate Status via domain method
        candidate.UpdateStatus(CandidateStatus.Onboarded);
        await _candidateRepo.UpdateAsync(candidate.Id, candidate, cancellationToken);

        // 5. Publish Event
        await _publisher.Publish(new DomainEventNotification<EmployeeCreatedEvent>(
            new EmployeeCreatedEvent(employee.Id, employee.FullName, employee.Email, employee.PersonalInfo?.Phone ?? string.Empty)), cancellationToken);

        await _unitOfWork.CommitTransactionAsync();
        return employee.Id;
      }
      catch (Exception)
      {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
      }
    }
  }
}

