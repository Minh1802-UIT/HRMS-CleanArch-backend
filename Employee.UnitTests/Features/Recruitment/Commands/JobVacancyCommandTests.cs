using Xunit;
using Moq;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Recruitment.Commands.JobVacancy.CreateJobVacancy;
using Employee.Application.Features.Recruitment.Commands.JobVacancy.DeleteJobVacancy;
using Employee.Application.Features.Recruitment.Commands.JobVacancy.CloseJobVacancy;
using Employee.Application.Features.Recruitment.Commands.JobVacancy.UpdateJobVacancy;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.UnitTests.Features.Recruitment.Commands
{
  public class JobVacancyCommandTests
  {
    private readonly Mock<IJobVacancyRepository> _mockRepo;

    private readonly CreateJobVacancyHandler _createHandler;
    private readonly DeleteJobVacancyHandler _deleteHandler;
    private readonly CloseJobVacancyHandler _closeHandler;
    private readonly UpdateJobVacancyHandler _updateHandler;

    public JobVacancyCommandTests()
    {
      _mockRepo = new Mock<IJobVacancyRepository>();
      _createHandler = new CreateJobVacancyHandler(_mockRepo.Object);
      _deleteHandler = new DeleteJobVacancyHandler(_mockRepo.Object);
      _closeHandler = new CloseJobVacancyHandler(_mockRepo.Object);
      _updateHandler = new UpdateJobVacancyHandler(_mockRepo.Object);
    }

    // --- Create JobVacancy ---

    [Fact]
    public async Task Create_Valid_ShouldPersistToRepository()
    {
      // Arrange
      var dto = new JobVacancyDto
      {
        Title = "Software Engineer",
        Description = "Backend developer",
        Vacancies = 2,
        ExpiredDate = DateTime.UtcNow.AddMonths(1),
        Requirements = new List<string> { "C#", ".NET" }
      };
      var command = new CreateJobVacancyCommand(dto);

      // Act
      await _createHandler.Handle(command, CancellationToken.None);

      // Assert
      _mockRepo.Verify(x => x.CreateAsync(It.IsAny<JobVacancy>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Delete JobVacancy ---

    [Fact]
    public async Task Delete_ShouldCallRepository()
    {
      // Arrange
      var command = new DeleteJobVacancyCommand("jv1");

      // Act
      await _deleteHandler.Handle(command, CancellationToken.None);

      // Assert
      _mockRepo.Verify(x => x.DeleteAsync("jv1", It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Close JobVacancy ---

    [Fact]
    public async Task Close_Existing_ShouldSetStatusToClosed()
    {
      // Arrange
      var vacancy = new JobVacancy("Software Engineer", 2, DateTime.UtcNow.AddMonths(1));
      _mockRepo.Setup(x => x.GetByIdAsync("jv1", It.IsAny<CancellationToken>())).ReturnsAsync(vacancy);

      var command = new CloseJobVacancyCommand("jv1");

      // Act
      await _closeHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal(JobVacancyStatus.Closed, vacancy.Status);
      _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<JobVacancy>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Close_NotFound_ShouldThrowNotFoundException()
    {
      // Arrange
      _mockRepo.Setup(x => x.GetByIdAsync("notexist", It.IsAny<CancellationToken>())).ReturnsAsync((JobVacancy?)null);

      var command = new CloseJobVacancyCommand("notexist");

      // Act & Assert
      await Assert.ThrowsAsync<NotFoundException>(() =>
          _closeHandler.Handle(command, CancellationToken.None));
    }

    // --- Update JobVacancy ---

    [Fact]
    public async Task Update_Valid_ShouldUpdateFields()
    {
      // Arrange
      var vacancy = new JobVacancy("Old Title", 1, DateTime.UtcNow.AddMonths(1));
      _mockRepo.Setup(x => x.GetByIdAsync("jv1", It.IsAny<CancellationToken>())).ReturnsAsync(vacancy);

      var updatedExpiry = DateTime.UtcNow.AddMonths(2);
      var dto = new JobVacancyDto
      {
        Title = "New Title",
        Description = "Updated description",
        Vacancies = 3,
        ExpiredDate = updatedExpiry,
        Requirements = new List<string> { "Go", "Kubernetes" }
      };
      var command = new UpdateJobVacancyCommand("jv1", dto);

      // Act
      await _updateHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal("New Title", vacancy.Title);
      Assert.Equal(3, vacancy.Vacancies);
      _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<JobVacancy>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_NotFound_ShouldThrowNotFoundException()
    {
      // Arrange
      _mockRepo.Setup(x => x.GetByIdAsync("notexist", It.IsAny<CancellationToken>())).ReturnsAsync((JobVacancy?)null);

      var dto = new JobVacancyDto { Title = "T", Vacancies = 1, ExpiredDate = DateTime.UtcNow.AddMonths(1) };
      var command = new UpdateJobVacancyCommand("notexist", dto);

      // Act & Assert
      await Assert.ThrowsAsync<NotFoundException>(() =>
          _updateHandler.Handle(command, CancellationToken.None));
    }
  }
}
