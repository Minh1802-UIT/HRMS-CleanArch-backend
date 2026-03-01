using Xunit;
using Moq;
using Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidateStatus;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;
using System;
using Employee.Application.Common.Exceptions;

namespace Employee.UnitTests.Features.Recruitment.Commands
{
  public class CandidateCommandTests
  {
    private readonly Mock<ICandidateRepository> _mockRepo;
    private readonly UpdateCandidateStatusHandler _statusHandler;

    public CandidateCommandTests()
    {
      _mockRepo = new Mock<ICandidateRepository>();
      _statusHandler = new UpdateCandidateStatusHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task UpdateStatus_Valid_ShouldUpdate()
    {
      // Arrange
      var id = "c1";
      var candidate = new Candidate("John", "john@test.com", "123", "vac1", System.DateTime.UtcNow);
      candidate.SetId(id);
      _mockRepo.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(candidate);

      var command = new UpdateCandidateStatusCommand(id, "Hired");

      // Act
      await _statusHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal(CandidateStatus.Hired, candidate.Status);
      _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Candidate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_Invalid_ShouldThrow()
    {
      // Arrange
      var id = "c1";
      var candidate = new Candidate("John", "john@test.com", "123", "vac1", System.DateTime.UtcNow);
      candidate.SetId(id);
      _mockRepo.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(candidate);

      var command = new UpdateCandidateStatusCommand(id, "InvalidStatus");

      // Act & Assert
      await Assert.ThrowsAsync<ValidationException>(() => _statusHandler.Handle(command, CancellationToken.None));
    }
  }
}

