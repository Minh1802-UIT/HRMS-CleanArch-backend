using Microsoft.Extensions.Configuration;
using Xunit;
using Moq;
using Employee.Application.Features.Auth.Commands.ForgotPassword;
using Employee.Application.Features.Auth.Commands.ResetPassword;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Dtos;
using System.Threading;
using System.Threading.Tasks;
using Employee.Application.Common.Exceptions;
using System;

namespace Employee.UnitTests.Features.Auth.Commands
{
    public class AuthCommandTests
    {
        private readonly Mock<IIdentityService> _mockIdentityService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ForgotPasswordCommandHandler _forgotHandler;
        private readonly ResetPasswordCommandHandler _resetHandler;

        public AuthCommandTests()
        {
            _mockIdentityService = new Mock<IIdentityService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();

            _forgotHandler = new ForgotPasswordCommandHandler(_mockIdentityService.Object, _mockEmailService.Object, _mockConfiguration.Object);
            _resetHandler = new ResetPasswordCommandHandler(_mockIdentityService.Object);
        }

        // --- Forgot Password Tests ---

        [Fact]
        public async Task ForgotPassword_ValidEmail_ShouldReturnSuccessMessage()
        {
            // Arrange
            var email = "user@example.com";
            var userDto = new UserDto { Email = email, Username = "user1", FullName = "User One" };
            var token = "reset_token";

            _mockIdentityService.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(userDto);
            _mockIdentityService.Setup(x => x.GenerateForgotPasswordTokenAsync(email)).ReturnsAsync(token);

            // Act
            var result = await _forgotHandler.Handle(new ForgotPasswordCommand { Email = email }, CancellationToken.None);

            // Assert
            Assert.Equal("Nếu email tồn tại trong hệ thống, mã đặt lại mật khẩu sẽ được gửi đến email của bạn.", result);
            _mockEmailService.Verify(x => x.SendAsync(email, It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_InvalidEmail_ShouldReturnNeutralMessage()
        {
            // Arrange — email không tồn tại, trả về thông báo trung lập (anti-enumeration)
            var email = "unknown@example.com";
            _mockIdentityService.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _forgotHandler.Handle(new ForgotPasswordCommand { Email = email }, CancellationToken.None);

            // Assert — không throw exception, trả về cùng thông báo trung lập
            Assert.Equal("Nếu email tồn tại trong hệ thống, mã đặt lại mật khẩu sẽ được gửi đến email của bạn.", result);
            _mockEmailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        // --- Reset Password Tests ---

        [Fact]
        public async Task ResetPassword_ValidToken_ShouldSucceed()
        {
            // Arrange
            var command = new ResetPasswordCommand { Email = "user@example.com", Token = "valid_token", NewPassword = "NewPassword123!" };

            _mockIdentityService.Setup(x => x.ResetPasswordAsync(command.Email, command.Token, command.NewPassword))
                .ReturnsAsync(Result.Success());

            // Act
            await _resetHandler.Handle(command, CancellationToken.None);

            // Assert
            _mockIdentityService.Verify(x => x.ResetPasswordAsync(command.Email, command.Token, command.NewPassword), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_ShouldThrow()
        {
            // Arrange
            var command = new ResetPasswordCommand { Email = "user@example.com", Token = "invalid_token", NewPassword = "NewPassword123!" };

            _mockIdentityService.Setup(x => x.ResetPasswordAsync(command.Email, command.Token, command.NewPassword))
                .ReturnsAsync(Result.Failure(new[] { "Invalid token" }));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _resetHandler.Handle(command, CancellationToken.None));
            Assert.Contains("Invalid token", ex.Message);
        }
    }
}
