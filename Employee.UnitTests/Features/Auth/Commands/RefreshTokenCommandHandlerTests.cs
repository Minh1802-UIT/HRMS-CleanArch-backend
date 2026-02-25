using Xunit;
using Moq;
using Employee.Application.Features.Auth.Commands.RefreshToken;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Dtos;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Employee.UnitTests.Features.Auth.Commands
{
  public class RefreshTokenCommandHandlerTests
  {
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
      _mockIdentityService = new Mock<IIdentityService>();
      _handler = new RefreshTokenCommandHandler(_mockIdentityService.Object);
    }

    // ─────────────────────────────────────────────
    // Happy path — valid token pair returns rotated LoginResponseDto
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidTokens_ShouldReturnLoginResponseDto()
    {
      // Arrange
      var command = new RefreshTokenCommand
      {
        AccessToken = "old-access-token",
        RefreshToken = "valid-refresh-token"
      };

      var expected = new LoginResponseDto
      {
        AccessToken = "new-access-token",
        RefreshToken = "new-refresh-token",
        TokenType = "Bearer",
        ExpiresIn = 3600,
        User = new UserDto { Username = "alice" }
      };

      _mockIdentityService
          .Setup(x => x.RefreshTokenAsync(command.AccessToken, command.RefreshToken))
          .ReturnsAsync(expected);

      // Act
      var result = await _handler.Handle(command, CancellationToken.None);

      // Assert
      Assert.Equal("new-access-token", result.AccessToken);
      Assert.Equal("new-refresh-token", result.RefreshToken);
      Assert.Equal("Bearer", result.TokenType);
      Assert.Equal(3600, result.ExpiresIn);
      Assert.Equal("alice", result.User.Username);
    }

    // ─────────────────────────────────────────────
    // Handler delegates to IIdentityService exactly once
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_CallsRefreshTokenAsyncOnce()
    {
      // Arrange
      var command = new RefreshTokenCommand
      {
        AccessToken = "at",
        RefreshToken = "rt"
      };

      _mockIdentityService
          .Setup(x => x.RefreshTokenAsync(command.AccessToken, command.RefreshToken))
          .ReturnsAsync(new LoginResponseDto { AccessToken = "new-at", RefreshToken = "new-rt" });

      // Act
      await _handler.Handle(command, CancellationToken.None);

      // Assert — identity service was called exactly once with the right arguments
      _mockIdentityService.Verify(
          x => x.RefreshTokenAsync(command.AccessToken, command.RefreshToken),
          Times.Once);
    }

    // ─────────────────────────────────────────────
    // Expired or unknown refresh token → UnauthorizedAccessException propagates
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExpiredToken_ShouldThrowUnauthorizedAccessException()
    {
      // Arrange
      var command = new RefreshTokenCommand
      {
        AccessToken = "any-access-token",
        RefreshToken = "expired-refresh-token"
      };

      _mockIdentityService
          .Setup(x => x.RefreshTokenAsync(command.AccessToken, command.RefreshToken))
          .ThrowsAsync(new UnauthorizedAccessException("Refresh token has expired. Please log in again."));

      // Act & Assert
      var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => _handler.Handle(command, CancellationToken.None));

      Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_UnknownToken_ShouldThrowUnauthorizedAccessException()
    {
      // Arrange
      var command = new RefreshTokenCommand
      {
        AccessToken = "any-access-token",
        RefreshToken = "unknown-or-invalid-token"
      };

      _mockIdentityService
          .Setup(x => x.RefreshTokenAsync(command.AccessToken, command.RefreshToken))
          .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token."));

      // Act & Assert
      var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => _handler.Handle(command, CancellationToken.None));

      Assert.Contains("Invalid", ex.Message);
    }

    // ─────────────────────────────────────────────
    // Reuse detection — stolen/replayed token → security exception propagates
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_RevokedToken_ShouldThrowWithSecurityMessage()
    {
      // Arrange
      var command = new RefreshTokenCommand
      {
        AccessToken = "any-access-token",
        RefreshToken = "already-revoked-token"
      };

      const string securityMsg = "Phát hiện token đã bị thu hồi — toàn bộ phiên đã bị hủy vì lý do bảo mật.";

      _mockIdentityService
          .Setup(x => x.RefreshTokenAsync(command.AccessToken, command.RefreshToken))
          .ThrowsAsync(new UnauthorizedAccessException(securityMsg));

      // Act & Assert
      var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => _handler.Handle(command, CancellationToken.None));

      Assert.Equal(securityMsg, ex.Message);
    }
  }
}
