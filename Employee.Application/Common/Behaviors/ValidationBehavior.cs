using FluentValidation;
using MediatR;

namespace Employee.Application.Common.Behaviors
{
  /// <summary>
  /// MediatR Pipeline Behavior that runs FluentValidation validators
  /// before the command/query handler executes.
  /// This ensures ALL requests sent via ISender are validated,
  /// not just those going through endpoint filters.
  /// </summary>
  public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
      where TRequest : notnull
  {
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
      _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
      if (!_validators.Any())
        return await next();

      var context = new ValidationContext<TRequest>(request);

      var validationResults = await Task.WhenAll(
          _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

      var failures = validationResults
          .SelectMany(r => r.Errors)
          .Where(f => f != null)
          .ToList();

      if (failures.Any())
      {
        var errorMessages = failures
            .Select(f => $"{f.PropertyName}: {f.ErrorMessage}")
            .ToList();

        throw new Exceptions.ValidationException(
            "One or more validation errors occurred.", errorMessages);
      }

      return await next();
    }
  }
}
