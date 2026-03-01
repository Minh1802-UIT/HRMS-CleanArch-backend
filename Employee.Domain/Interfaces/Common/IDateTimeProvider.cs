namespace Employee.Domain.Interfaces.Common
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
