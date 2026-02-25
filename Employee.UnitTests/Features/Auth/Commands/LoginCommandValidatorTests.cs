using Employee.Application.Features.Auth.Commands.Login;
using FluentValidation.TestHelper;
using Xunit;

namespace Employee.UnitTests.Features.Auth.Commands
{
  public class LoginCommandValidatorTests
  {
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
      _validator = new LoginCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Username_Is_Empty()
    {
      var command = new LoginCommand { Username = "", Password = "password123" };
      var result = _validator.TestValidate(command);
      result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
      var command = new LoginCommand { Username = "admin", Password = "" };
      var result = _validator.TestValidate(command);
      result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Password_Too_Short()
    {
      var command = new LoginCommand { Username = "admin", Password = "abc" };
      var result = _validator.TestValidate(command);
      result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters.");
    }

    [Fact]
    public void Should_Have_Error_When_Username_Exceeds_MaxLength()
    {
      var command = new LoginCommand
      {
        Username = new string('a', 101),
        Password = "password123"
      };
      var result = _validator.TestValidate(command);
      result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must not exceed 100 characters.");
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
      var command = new LoginCommand { Username = "admin", Password = "StrongPass1" };
      var result = _validator.TestValidate(command);
      result.ShouldNotHaveValidationErrorFor(x => x.Username);
      result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
  }
}
