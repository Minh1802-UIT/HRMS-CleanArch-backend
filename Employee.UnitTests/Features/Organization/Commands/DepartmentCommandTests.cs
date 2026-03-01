using Xunit;
using Moq;
using Employee.Application.Features.Organization.Commands.CreateDepartment;
using Employee.Application.Features.Organization.Commands.UpdateDepartment;
using Employee.Application.Features.Organization.Commands.DeleteDepartment;
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
  public class DepartmentCommandTests
  {
    private readonly Mock<IDepartmentRepository> _mockRepo;
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<IEmployeeRepository> _mockEmpRepo;
    private readonly CreateDepartmentHandler _createHandler;
    private readonly UpdateDepartmentHandler _updateHandler;
    private readonly DeleteDepartmentHandler _deleteHandler;

    public DepartmentCommandTests()
    {
      _mockRepo = new Mock<IDepartmentRepository>();
      _mockCache = new Mock<ICacheService>();
      _mockEmpRepo = new Mock<IEmployeeRepository>();

      _createHandler = new CreateDepartmentHandler(_mockRepo.Object, _mockCache.Object);
      _updateHandler = new UpdateDepartmentHandler(_mockRepo.Object, _mockCache.Object);
      _deleteHandler = new DeleteDepartmentHandler(_mockRepo.Object, _mockCache.Object, _mockEmpRepo.Object);
    }

    [Fact]
    public async Task Create_Valid_ShouldCreateAndClearCache()
    {
      // Arrange
      var dto = new CreateDepartmentDto { Name = "IT", Code = "IT_01" };
      var command = new CreateDepartmentCommand(dto);

      // Act
      await _createHandler.Handle(command, CancellationToken.None);

      // Assert
      _mockRepo.Verify(x => x.CreateAsync(It.Is<Department>(d => d.Name == "IT"), It.IsAny<CancellationToken>()), Times.Once);
      _mockCache.Verify(x => x.RemoveAsync("DEPARTMENT_TREE"), Times.Once);
    }

    [Fact]
    public async Task Delete_HasEmployees_ShouldThrow()
    {
      // Arrange
      var id = "dept_1";
      var command = new DeleteDepartmentCommand(id);
      _mockEmpRepo.Setup(x => x.ExistsByDepartmentIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

      // Act & Assert
      await Assert.ThrowsAsync<ValidationException>(() => _deleteHandler.Handle(command, CancellationToken.None));
      _mockRepo.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
  }
}
