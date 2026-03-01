using Xunit;
using Moq;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Performance.Commands.CreatePerformanceGoal;
using Employee.Application.Features.Performance.Commands.CreatePerformanceReview;
using Employee.Application.Features.Performance.Commands.UpdatePerformanceGoalProgress;
using Employee.Application.Features.Performance.Commands.UpdatePerformanceReview;
using Employee.Application.Features.Performance.Dtos;
using Employee.Domain.Entities.Performance;
using Employee.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.UnitTests.Features.Performance
{
  public class PerformanceCommandTests
  {
    private readonly Mock<IPerformanceGoalRepository> _mockGoalRepo;
    private readonly Mock<IPerformanceReviewRepository> _mockReviewRepo;

    private readonly CreatePerformanceGoalHandler _createGoalHandler;
    private readonly CreatePerformanceReviewHandler _createReviewHandler;
    private readonly UpdatePerformanceGoalProgressHandler _updateProgressHandler;
    private readonly UpdatePerformanceReviewHandler _updateReviewHandler;

    public PerformanceCommandTests()
    {
      _mockGoalRepo = new Mock<IPerformanceGoalRepository>();
      _mockReviewRepo = new Mock<IPerformanceReviewRepository>();

      _createGoalHandler = new CreatePerformanceGoalHandler(_mockGoalRepo.Object);
      _createReviewHandler = new CreatePerformanceReviewHandler(_mockReviewRepo.Object);
      _updateProgressHandler = new UpdatePerformanceGoalProgressHandler(_mockGoalRepo.Object);
      _updateReviewHandler = new UpdatePerformanceReviewHandler(_mockReviewRepo.Object);
    }

    // --- CreatePerformanceGoal ---

    [Fact]
    public async Task CreateGoal_Valid_ShouldPersistAndReturnId()
    {
      // Arrange
      var dto = new PerformanceGoalDto
      {
        EmployeeId = "emp1",
        Title = "Improve productivity",
        Description = "Increase output by 20%",
        TargetDate = DateTime.UtcNow.AddMonths(3)
      };
      var command = new CreatePerformanceGoalCommand(dto);
      _mockGoalRepo.Setup(x => x.CreateAsync(It.IsAny<PerformanceGoal>(), It.IsAny<CancellationToken>()))
          .Callback<PerformanceGoal, CancellationToken>((g, _) => g.SetId("goal-1"));

      // Act
      var result = await _createGoalHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal("goal-1", result);
      _mockGoalRepo.Verify(x => x.CreateAsync(It.IsAny<PerformanceGoal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateGoal_MissingEmployeeId_ShouldThrow()
    {
      // Arrange
      var dto = new PerformanceGoalDto
      {
        EmployeeId = "",
        Title = "Title",
        TargetDate = DateTime.UtcNow.AddMonths(1)
      };
      var command = new CreatePerformanceGoalCommand(dto);

      // Act & Assert
      await Assert.ThrowsAsync<ArgumentException>(() =>
          _createGoalHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateGoal_MissingTitle_ShouldThrow()
    {
      // Arrange
      var dto = new PerformanceGoalDto
      {
        EmployeeId = "emp1",
        Title = "",
        TargetDate = DateTime.UtcNow.AddMonths(1)
      };
      var command = new CreatePerformanceGoalCommand(dto);

      // Act & Assert
      await Assert.ThrowsAsync<ArgumentException>(() =>
          _createGoalHandler.Handle(command, CancellationToken.None));
    }

    // --- CreatePerformanceReview ---

    [Fact]
    public async Task CreateReview_Valid_ShouldPersistAndReturnId()
    {
      // Arrange
      var start = DateTime.UtcNow.AddMonths(-1);
      var end = DateTime.UtcNow;
      var dto = new PerformanceReviewDto
      {
        EmployeeId = "emp1",
        ReviewerId = "mgr1",
        PeriodStart = start,
        PeriodEnd = end
      };
      var command = new CreatePerformanceReviewCommand(dto);
      _mockReviewRepo.Setup(x => x.CreateAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()))
          .Callback<PerformanceReview, CancellationToken>((r, _) => r.SetId("review-1"));

      // Act
      var result = await _createReviewHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal("review-1", result);
      _mockReviewRepo.Verify(x => x.CreateAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReview_PeriodEndBeforeStart_ShouldThrow()
    {
      // Arrange
      var dto = new PerformanceReviewDto
      {
        EmployeeId = "emp1",
        ReviewerId = "mgr1",
        PeriodStart = DateTime.UtcNow,
        PeriodEnd = DateTime.UtcNow.AddDays(-1) // invalid
      };
      var command = new CreatePerformanceReviewCommand(dto);

      // Act & Assert
      await Assert.ThrowsAsync<ArgumentException>(() =>
          _createReviewHandler.Handle(command, CancellationToken.None));
    }

    // --- UpdatePerformanceGoalProgress ---

    [Fact]
    public async Task UpdateProgress_Valid_ShouldReturnTrue()
    {
      // Arrange
      var goal = new PerformanceGoal("emp1", "Title", "Desc", DateTime.UtcNow.AddMonths(3));
      _mockGoalRepo.Setup(x => x.GetByIdAsync("g1", It.IsAny<CancellationToken>())).ReturnsAsync(goal);

      var command = new UpdatePerformanceGoalProgressCommand("g1", 75.0);

      // Act
      var result = await _updateProgressHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.True(result);
      Assert.Equal(75.0, goal.Progress);
      _mockGoalRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<PerformanceGoal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProgress_GoalNotFound_ShouldReturnFalse()
    {
      // Arrange
      _mockGoalRepo.Setup(x => x.GetByIdAsync("notexist", It.IsAny<CancellationToken>())).ReturnsAsync((PerformanceGoal?)null);

      var command = new UpdatePerformanceGoalProgressCommand("notexist", 50.0);

      // Act
      var result = await _updateProgressHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.False(result);
      _mockGoalRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<PerformanceGoal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProgress_100Percent_ShouldMarkCompleted()
    {
      // Arrange
      var goal = new PerformanceGoal("emp1", "Title", "Desc", DateTime.UtcNow.AddMonths(3));
      _mockGoalRepo.Setup(x => x.GetByIdAsync("g1", It.IsAny<CancellationToken>())).ReturnsAsync(goal);

      var command = new UpdatePerformanceGoalProgressCommand("g1", 100.0);

      // Act
      await _updateProgressHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal(PerformanceGoalStatus.Completed, goal.Status);
    }

    // --- UpdatePerformanceReview ---

    [Fact]
    public async Task UpdateReview_Valid_ShouldReturnTrue()
    {
      // Arrange
      var review = new PerformanceReview("emp1", "mgr1",
          DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
      _mockReviewRepo.Setup(x => x.GetByIdAsync("r1", It.IsAny<CancellationToken>())).ReturnsAsync(review);

      var dto = new PerformanceReviewDto
      {
        OverallScore = 85.0,
        Notes = "Excellent performance",
        Status = PerformanceReviewStatus.Completed
      };
      var command = new UpdatePerformanceReviewCommand("r1", dto);

      // Act
      var result = await _updateReviewHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.True(result);
      Assert.Equal(85.0, review.OverallScore);
      Assert.Equal("Excellent performance", review.Notes);
      _mockReviewRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReview_NotFound_ShouldReturnFalse()
    {
      // Arrange
      _mockReviewRepo.Setup(x => x.GetByIdAsync("notexist", It.IsAny<CancellationToken>())).ReturnsAsync((PerformanceReview?)null);

      var dto = new PerformanceReviewDto { OverallScore = 90.0, Status = PerformanceReviewStatus.Completed };
      var command = new UpdatePerformanceReviewCommand("notexist", dto);

      // Act
      var result = await _updateReviewHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.False(result);
      _mockReviewRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateReview_InvalidScore_ShouldThrow()
    {
      // Arrange
      var review = new PerformanceReview("emp1", "mgr1",
          DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
      _mockReviewRepo.Setup(x => x.GetByIdAsync("r1", It.IsAny<CancellationToken>())).ReturnsAsync(review);

      var dto = new PerformanceReviewDto { OverallScore = 150.0, Status = PerformanceReviewStatus.Draft }; // invalid
      var command = new UpdatePerformanceReviewCommand("r1", dto);

      // Act & Assert
      await Assert.ThrowsAsync<ArgumentException>(() =>
          _updateReviewHandler.Handle(command, CancellationToken.None));
    }
  }
}
