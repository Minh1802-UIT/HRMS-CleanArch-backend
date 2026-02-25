using System;
using System.Collections.Generic;

namespace Employee.Domain.Entities.Common
{
  public abstract class BaseEntity
  {
    // We use string for Id in the Domain.
    // MongoDbConfig in the Infrastructure layer handles the mapping to ObjectId.
    public string Id { get; private set; } = null!;
    public bool IsDeleted { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public string CreatedBy { get; private set; } = "System";
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public int Version { get; private set; } = 1;

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void SetId(string id) => Id = id;
    public void SetVersion(int version) => Version = version;
    public void SetUpdatedAt(DateTime updatedAt, string? updatedBy = null)
    {
      UpdatedAt = updatedAt;
      UpdatedBy = updatedBy;
    }

    public void SetCreatedInfo(DateTime createdAt, string createdBy)
    {
      CreatedAt = createdAt;
      CreatedBy = createdBy;
    }

    public void MarkDeleted(string? deletedBy = null)
    {
      IsDeleted = true;
      UpdatedAt = DateTime.UtcNow;
      UpdatedBy = deletedBy ?? UpdatedBy;
    }

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
  }
}