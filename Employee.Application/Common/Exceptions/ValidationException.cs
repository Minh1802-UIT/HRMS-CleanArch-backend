using System;

namespace Employee.Application.Common.Exceptions
{
  public class ValidationException : Exception
  {
    public List<string>? Errors { get; }

    public ValidationException() : base() { }

    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, List<string> errors) : base(message)
    {
      Errors = errors;
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
  }
}
