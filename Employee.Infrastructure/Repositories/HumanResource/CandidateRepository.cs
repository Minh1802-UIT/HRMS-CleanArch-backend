using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Common;
using MongoDB.Driver;

using Employee.Infrastructure.Persistence;

namespace Employee.Infrastructure.Repositories.HumanResource
{
  public class CandidateRepository : ICandidateRepository
  {
    private readonly IMongoCollection<Candidate> _collection;
    private readonly IMongoContext _context;

    public CandidateRepository(IMongoContext context)
    {
      _context = context;
      _collection = _context.GetCollection<Candidate>("candidates");
    }

    public async Task<IEnumerable<Candidate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task<Candidate?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.Id == id && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Candidate>> GetByVacancyIdAsync(string vacancyId, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.JobVacancyId == vacancyId && x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Candidate entity, CancellationToken cancellationToken = default)
    {
      await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Candidate entity, CancellationToken cancellationToken = default)
    {
      await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
      var update = Builders<Candidate>.Update
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
