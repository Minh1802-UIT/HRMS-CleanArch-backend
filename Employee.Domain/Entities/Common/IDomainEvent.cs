namespace Employee.Domain.Entities.Common
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
