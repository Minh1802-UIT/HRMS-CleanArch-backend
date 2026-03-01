using Xunit;
using Moq;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Recruitment.Commands.OnboardCandidate;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.UnitTests.Features.Recruitment.Commands
{
    public class OnboardCandidateCommandTests
    {
        private readonly Mock<ICandidateRepository> _mockCandidateRepo;
        private readonly Mock<IEmployeeRepository> _mockEmployeeRepo;
        private readonly Mock<IPublisher> _mockPublisher;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        private readonly OnboardCandidateHandler _handler;

        public OnboardCandidateCommandTests()
        {
            _mockCandidateRepo = new Mock<ICandidateRepository>();
            _mockEmployeeRepo = new Mock<IEmployeeRepository>();
            _mockPublisher = new Mock<IPublisher>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _handler = new OnboardCandidateHandler(
                _mockCandidateRepo.Object,
                _mockEmployeeRepo.Object,
                _mockPublisher.Object,
                _mockUnitOfWork.Object);
        }

        private Candidate CreateHiredCandidate(string id)
        {
            var candidate = new Candidate("Alice Nguyen", "alice@test.com", "0901234567", "jv1", System.DateTime.UtcNow);
            candidate.SetId(id);
            candidate.UpdateStatus(CandidateStatus.Interviewing);
            candidate.UpdateStatus(CandidateStatus.Hired);
            return candidate;
        }

        [Fact]
        public async Task Onboard_HiredCandidate_ShouldCreateEmployeeAndReturnId()
        {
            // Arrange
            var candidate = CreateHiredCandidate("cand1");
            _mockCandidateRepo.Setup(x => x.GetByIdAsync("cand1", It.IsAny<CancellationToken>())).ReturnsAsync(candidate);
            _mockEmployeeRepo.Setup(x => x.CreateAsync(It.IsAny<EmployeeEntity>(), It.IsAny<CancellationToken>()))
                .Callback<EmployeeEntity, CancellationToken>((e, _) => e.SetId("emp-new-1"));

            var command = new OnboardCandidateCommand
            {
                CandidateId = "cand1",
                OnboardData = new OnboardCandidateDto
                {
                    EmployeeCode = "EMP999",
                    DepartmentId = "dept1",
                    PositionId = "pos1",
                    JoinDate = DateTime.UtcNow,
                    DateOfBirth = new DateTime(1995, 6, 15)
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("emp-new-1", result);
            _mockEmployeeRepo.Verify(x => x.CreateAsync(It.IsAny<EmployeeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockCandidateRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Candidate>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            Assert.Equal(CandidateStatus.Onboarded, candidate.Status);
        }

        [Fact]
        public async Task Onboard_CandidateNotFound_ShouldThrowValidationException()
        {
            // Arrange
            _mockCandidateRepo.Setup(x => x.GetByIdAsync("notexist", It.IsAny<CancellationToken>())).ReturnsAsync((Candidate?)null);

            var command = new OnboardCandidateCommand
            {
                CandidateId = "notexist",
                OnboardData = new OnboardCandidateDto { EmployeeCode = "EMP001", DepartmentId = "d1", PositionId = "p1", DateOfBirth = new DateTime(1990, 1, 1) }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(command, CancellationToken.None));

            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Onboard_NonHiredCandidate_ShouldThrowValidationException()
        {
            // Arrange
            var candidate = new Candidate("Bob", "bob@test.com", "0909", "jv1", System.DateTime.UtcNow);
            candidate.SetId("cand2");
            // Status is Applied by default
            _mockCandidateRepo.Setup(x => x.GetByIdAsync("cand2", It.IsAny<CancellationToken>())).ReturnsAsync(candidate);

            var command = new OnboardCandidateCommand
            {
                CandidateId = "cand2",
                OnboardData = new OnboardCandidateDto { EmployeeCode = "EMP002", DepartmentId = "d1", PositionId = "p1", DateOfBirth = new DateTime(1990, 1, 1) }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(command, CancellationToken.None));

            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Onboard_WhenCreateEmployeeFails_ShouldRollbackTransaction()
        {
            // Arrange
            var candidate = CreateHiredCandidate("cand3");
            _mockCandidateRepo.Setup(x => x.GetByIdAsync("cand3", It.IsAny<CancellationToken>())).ReturnsAsync(candidate);
            _mockEmployeeRepo.Setup(x => x.CreateAsync(It.IsAny<EmployeeEntity>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));

            var command = new OnboardCandidateCommand
            {
                CandidateId = "cand3",
                OnboardData = new OnboardCandidateDto
                {
                    EmployeeCode = "EMP003",
                    DepartmentId = "dept1",
                    PositionId = "pos1",
                    JoinDate = DateTime.UtcNow,
                    DateOfBirth = new DateTime(1993, 3, 20)
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(command, CancellationToken.None));

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
        }
    }
}

