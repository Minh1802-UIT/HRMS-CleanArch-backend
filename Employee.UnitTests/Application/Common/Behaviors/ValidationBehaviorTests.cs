using FluentValidation;
using MediatR;
using Employee.Application.Common.Behaviors;
using System.Threading;
using System.Threading.Tasks;
using ValidationException = Employee.Application.Common.Exceptions.ValidationException;

namespace Employee.UnitTests.Application.Common.Behaviors
{
  // ─── public stub types — must be public so Castle.DynamicProxy can see them ──
  public class VbTestRequest : IRequest<string>
  {
    public string Value { get; set; } = string.Empty;
  }

  /// <summary>Always passes — no rules defined.</summary>
  public class AlwaysPassValidator : AbstractValidator<VbTestRequest> { }

  /// <summary>Always fails with a single configurable message.</summary>
  public class AlwaysFailValidator : AbstractValidator<VbTestRequest>
  {
    public AlwaysFailValidator(string propertyName, string message)
    {
      RuleFor(r => r.Value).Must(_ => false).WithName(propertyName).WithMessage(message);
    }
  }

  public class ValidationBehaviorTests
  {
    // ─── helpers ──────────────────────────────────────────────────────────

    private static RequestHandlerDelegate<string> NextReturns(string value)
        => () => Task.FromResult(value);

    // ─── tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
      var behavior = new ValidationBehavior<VbTestRequest, string>(
          Enumerable.Empty<IValidator<VbTestRequest>>());

      var result = await behavior.Handle(new VbTestRequest(), NextReturns("ok"), CancellationToken.None);

      Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_WithPassingValidator_ShouldCallNext()
    {
      var behavior = new ValidationBehavior<VbTestRequest, string>(
          new IValidator<VbTestRequest>[] { new AlwaysPassValidator() });

      var result = await behavior.Handle(new VbTestRequest(), NextReturns("passed"), CancellationToken.None);

      Assert.Equal("passed", result);
    }

    [Fact]
    public async Task Handle_WithSingleFailingValidator_ShouldThrowValidationException()
    {
      var behavior = new ValidationBehavior<VbTestRequest, string>(
          new IValidator<VbTestRequest>[] { new AlwaysFailValidator("Value", "Value is required") });

      var ex = await Assert.ThrowsAsync<ValidationException>(
          () => behavior.Handle(new VbTestRequest(), NextReturns("never"), CancellationToken.None));

      Assert.NotNull(ex.Errors);
      Assert.Single(ex.Errors!);
      Assert.Contains("Value is required", ex.Errors![0]);
    }

    [Fact]
    public async Task Handle_WithMultipleFailingValidators_ShouldAggregateAllErrors()
    {
      var behavior = new ValidationBehavior<VbTestRequest, string>(
          new IValidator<VbTestRequest>[]
          {
            new AlwaysFailValidator("Value", "Error A"),
            new AlwaysFailValidator("Value", "Error B")
          });

      var ex = await Assert.ThrowsAsync<ValidationException>(
          () => behavior.Handle(new VbTestRequest(), NextReturns("never"), CancellationToken.None));

      // Both validators contribute failures; exact count may vary with context
      // accumulation, but every declared error message must appear at least once.
      Assert.True(ex.Errors!.Count >= 2, $"Expected at least 2 errors, got {ex.Errors.Count}");
      Assert.Contains(ex.Errors, e => e.Contains("Error A"));
      Assert.Contains(ex.Errors, e => e.Contains("Error B"));
    }

    [Fact]
    public async Task Handle_WithMixedValidators_OnlyFailingOneTriggersException()
    {
      var behavior = new ValidationBehavior<VbTestRequest, string>(
          new IValidator<VbTestRequest>[]
          {
            new AlwaysPassValidator(),
            new AlwaysFailValidator("Value", "Must not be empty"),
            new AlwaysPassValidator()
          });

      var ex = await Assert.ThrowsAsync<ValidationException>(
          () => behavior.Handle(new VbTestRequest(), NextReturns("never"), CancellationToken.None));

      Assert.Contains(ex.Errors!, e => e.Contains("Must not be empty"));
    }

    [Fact]
    public async Task Handle_ValidationException_ContainsPropertyNameInMessage()
    {
      var behavior = new ValidationBehavior<VbTestRequest, string>(
          new IValidator<VbTestRequest>[] { new AlwaysFailValidator("Username", "Username is required.") });

      var ex = await Assert.ThrowsAsync<ValidationException>(
          () => behavior.Handle(new VbTestRequest(), NextReturns("never"), CancellationToken.None));

      Assert.Contains(ex.Errors!, e => e.Contains("Username") && e.Contains("Username is required."));
    }

    [Fact]
    public async Task Handle_NextIsNeverCalled_WhenValidationFails()
    {
      bool nextCalled = false;
      RequestHandlerDelegate<string> next = () => { nextCalled = true; return Task.FromResult("x"); };

      var behavior = new ValidationBehavior<VbTestRequest, string>(
          new IValidator<VbTestRequest>[] { new AlwaysFailValidator("Value", "fail") });

      await Assert.ThrowsAsync<ValidationException>(
          () => behavior.Handle(new VbTestRequest(), next, CancellationToken.None));

      Assert.False(nextCalled);
    }
  }
}
