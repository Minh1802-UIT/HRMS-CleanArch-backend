using Xunit;
using Moq;
using Employee.Application.Features.Auth.Commands.Login;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Dtos;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Employee.UnitTests.Features.Auth.Commands
{
  public class LoginCommandHandlerTests
  {
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
      _mockIdentityService = new Mock<IIdentityService>();
      _handler = new LoginCommandHandler(_mockIdentityService.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnToken()
    {
      // Arrange
      var request = new LoginCommand { Username = "user1", Password = "password" };
      var loginResponse = new LoginResponseDto 
      { 
          AccessToken = "mock_token", 
          RefreshToken = "mock_refresh_token",
          ExpiresIn = 86400,
          User = new UserDto { Username = "user1" } 
      };

      _mockIdentityService.Setup(x => x.LoginAsync(request.Username, request.Password))
          .ReturnsAsync(loginResponse);

      // Act
      var result = await _handler.Handle(request, CancellationToken.None);

      // Assert
      Assert.Equal("mock_token", result.AccessToken);
      Assert.Equal("mock_refresh_token", result.RefreshToken);
      Assert.Equal("user1", result.User.Username);
      _mockIdentityService.Verify(x => x.LoginAsync(request.Username, request.Password), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ShouldThrow()
    {
      // Arrange
      var request = new LoginCommand { Username = "unknown", Password = "password" };
      _mockIdentityService.Setup(x => x.LoginAsync(request.Username, request.Password))
          .ThrowsAsync(new Exception("Tài khoản không tồn tại."));

      // Act & Assert
      var ex = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(request, CancellationToken.None));
      Assert.Equal("Tài khoản không tồn tại.", ex.Message);
    }
  }
}
