using Xunit;
using Moq;
using Employee.Application.Features.Organization.Commands.CreatePosition;
using Employee.Application.Features.Organization.Commands.UpdatePosition;
using Employee.Application.Features.Organization.Commands.DeletePosition;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Entities.Organization;
using Employee.Application.Features.Organization.Dtos;
using System.Threading;
using System.Threading.Tasks;
using System;
using Employee.Application.Common.Exceptions;

namespace Employee.UnitTests.Features.Organization.Commands
{
  public class PositionCommandTests
  {
    private readonly Mock<IPositionRepository> _mockRepo;
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<IEmployeeRepository> _mockEmpRepo;
    private readonly Mock<IDepartmentRepository> _mockDeptRepo;
    private readonly CreatePositionHandler _createHandler;
    private readonly UpdatePositionHandler _updateHandler;
    private readonly DeletePositionHandler _deleteHandler;

    public PositionCommandTests()
    {
      _mockRepo = new Mock<IPositionRepository>();
      _mockCache = new Mock<ICacheService>();
      _mockEmpRepo = new Mock<IEmployeeRepository>();
      _mockDeptRepo = new Mock<IDepartmentRepository>();

      _createHandler = new CreatePositionHandler(_mockRepo.Object, _mockCache.Object, _mockDeptRepo.Object);
      _updateHandler = new UpdatePositionHandler(_mockRepo.Object, _mockCache.Object, _mockDeptRepo.Object);
      _deleteHandler = new DeletePositionHandler(_mockRepo.Object, _mockCache.Object, _mockEmpRepo.Object);
    }

    [Fact]
    public async Task Create_Valid_ShouldCreateAndClearCache()
    {
      // Arrange
      var dto = new CreatePositionDto { Title = "Dev", Code = "DEV", DepartmentId = "D1" };
      var command = new CreatePositionCommand(dto);
      _mockDeptRepo.Setup(x => x.GetByIdAsync("D1", It.IsAny<CancellationToken>())).ReturnsAsync(new Department("IT", "IT"));

      // Act
      await _createHandler.Handle(command, CancellationToken.None);

      // Assert
      _mockRepo.Verify(x => x.CreateAsync(It.Is<Position>(p => p.Title == "Dev"), It.IsAny<CancellationToken>()), Times.Once);
      _mockCache.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Delete_WithEmployees_ShouldThrowException()
    {
      // Arrange
      var id = "pos-1";
      var command = new DeletePositionCommand(id);
      _mockEmpRepo.Setup(x => x.ExistsByPositionIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

      // Act & Assert
      var ex = await Assert.ThrowsAsync<ValidationException>(() => _deleteHandler.Handle(command, CancellationToken.None));
      Assert.Contains("active employees", ex.Message);
    }

    [Fact]
    public async Task Delete_WithChildren_ShouldThrowException()
    {
      // Arrange
      var id = "pos-parent";
      var command = new DeletePositionCommand(id);
      _mockEmpRepo.Setup(x => x.ExistsByPositionIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
      var child = new Position("Child", "C", "D1");
      child.SetParent(id);
      var allPositions = new List<Position> { child };
      _mockRepo.Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(allPositions);

      // Act & Assert
      var ex = await Assert.ThrowsAsync<ValidationException>(() => _deleteHandler.Handle(command, CancellationToken.None));
      Assert.Contains("child positions", ex.Message);
    }
  }
}
