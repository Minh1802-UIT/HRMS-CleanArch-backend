using Xunit;
using Moq;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Recruitment.Commands.Interview.CreateInterview;
using Employee.Application.Features.Recruitment.Commands.Interview.DeleteInterview;
using Employee.Application.Features.Recruitment.Commands.Interview.ReviewInterview;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.UnitTests.Features.Recruitment.Commands
{
  public class InterviewCommandTests
  {
    private readonly Mock<IInterviewRepository> _mockRepo;

    private readonly CreateInterviewHandler _createHandler;
    private readonly DeleteInterviewHandler _deleteHandler;
    private readonly ReviewInterviewHandler _reviewHandler;

    public InterviewCommandTests()
    {
      _mockRepo = new Mock<IInterviewRepository>();
      _createHandler = new CreateInterviewHandler(_mockRepo.Object);
      _deleteHandler = new DeleteInterviewHandler(_mockRepo.Object);
      _reviewHandler = new ReviewInterviewHandler(_mockRepo.Object);
    }

    // --- Create Interview ---

    [Fact]
    public async Task Create_Valid_ShouldCallRepository()
    {
      // Arrange
      var dto = new InterviewDto
      {
        CandidateId = "cand1",
        InterviewerId = "emp1",
        ScheduledTime = DateTime.UtcNow.AddDays(3),
        DurationMinutes = 60,
        Location = "Online"
      };
      var command = new CreateInterviewCommand(dto);

      // Act
      await _createHandler.Handle(command, CancellationToken.None);

      // Assert
      _mockRepo.Verify(x => x.CreateAsync(It.IsAny<Interview>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Delete Interview ---

    [Fact]
    public async Task Delete_ShouldCallRepository()
    {
      // Arrange
      var command = new DeleteInterviewCommand("interview1");

      // Act
      await _deleteHandler.Handle(command, CancellationToken.None);

      // Assert
      _mockRepo.Verify(x => x.DeleteAsync("interview1", It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Review Interview ---

    [Fact]
    public async Task Review_Complete_ShouldSetCompletedStatus()
    {
      // Arrange
      var interview = new Interview("cand1", "emp1", DateTime.UtcNow.AddDays(1));
      _mockRepo.Setup(x => x.GetByIdAsync("i1", It.IsAny<CancellationToken>())).ReturnsAsync(interview);

      var dto = new ReviewInterviewDto { Result = "Completed", Notes = "Good candidate" };
      var command = new ReviewInterviewCommand("i1", dto);

      // Act
      await _reviewHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal(InterviewStatus.Completed, interview.Status);
      Assert.Equal("Good candidate", interview.Feedback);
      _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<Interview>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Review_Cancel_ShouldSetCancelledStatus()
    {
      // Arrange
      var interview = new Interview("cand1", "emp1", DateTime.UtcNow.AddDays(1));
      _mockRepo.Setup(x => x.GetByIdAsync("i1", It.IsAny<CancellationToken>())).ReturnsAsync(interview);

      var dto = new ReviewInterviewDto { Result = "Cancelled", Notes = "" };
      var command = new ReviewInterviewCommand("i1", dto);

      // Act
      await _reviewHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal(InterviewStatus.Cancelled, interview.Status);
    }

    [Fact]
    public async Task Review_NotFound_ShouldThrowNotFoundException()
    {
      // Arrange
      _mockRepo.Setup(x => x.GetByIdAsync("notexist", It.IsAny<CancellationToken>())).ReturnsAsync((Interview?)null);

      var dto = new ReviewInterviewDto { Result = "Completed", Notes = "" };
      var command = new ReviewInterviewCommand("notexist", dto);

      // Act & Assert
      await Assert.ThrowsAsync<NotFoundException>(() =>
          _reviewHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Review_InvalidStatus_ShouldThrowValidationException()
    {
      // Arrange
      var interview = new Interview("cand1", "emp1", DateTime.UtcNow.AddDays(1));
      _mockRepo.Setup(x => x.GetByIdAsync("i1", It.IsAny<CancellationToken>())).ReturnsAsync(interview);

      var dto = new ReviewInterviewDto { Result = "BadStatus", Notes = "" };
      var command = new ReviewInterviewCommand("i1", dto);

      // Act & Assert
      await Assert.ThrowsAsync<ValidationException>(() =>
          _reviewHandler.Handle(command, CancellationToken.None));
    }
  }
}
