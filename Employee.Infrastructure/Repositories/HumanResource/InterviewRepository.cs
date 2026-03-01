using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Common;
using MongoDB.Driver;
using System;

using Employee.Infrastructure.Persistence;

namespace Employee.Infrastructure.Repositories.HumanResource
{
  public class InterviewRepository : IInterviewRepository
  {
    private readonly IMongoCollection<Interview> _collection;
    private readonly IMongoContext _context;

    public InterviewRepository(IMongoContext context)
    {
      _context = context;
      _collection = _context.GetCollection<Interview>("interviews");
    }

    public async Task<IEnumerable<Interview>> GetAllAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Interview>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
      var start = date.Date;
      var end = start.AddDays(1);
      return await _collection.Find(x => x.ScheduledTime >= start && x.ScheduledTime < end).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Interview>> GetByCandidateIdAsync(string candidateId, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.CandidateId == candidateId && x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task<Interview?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.Id == id && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(Interview entity, CancellationToken cancellationToken = default)
    {
      await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Interview entity, CancellationToken cancellationToken = default)
    {
      await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
      var update = Builders<Interview>.Update
          .Set(x => x.IsDeleted, true)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);
      await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
      await _collection.DeleteManyAsync(_ => true, cancellationToken);
    }
  }
}
