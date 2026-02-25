using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Common;
using Employee.Application.Common.Models;
using MongoDB.Driver;

using Employee.Infrastructure.Persistence;

namespace Employee.Infrastructure.Repositories.HumanResource
{
  public class JobVacancyRepository : IJobVacancyRepository
  {
    private readonly IMongoCollection<JobVacancy> _collection;
    private readonly IMongoContext _context;

    public JobVacancyRepository(IMongoContext context)
    {
      _context = context;
      _collection = _context.GetCollection<JobVacancy>("job_vacancies");
    }

    public async Task<IEnumerable<JobVacancy>> GetAllAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task<JobVacancy?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.Id == id && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(JobVacancy entity, CancellationToken cancellationToken = default)
    {
      await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(JobVacancy entity, CancellationToken cancellationToken = default)
    {
      await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
      var update = Builders<JobVacancy>.Update
          .Set(x => x.IsDeleted, true)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);
      await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<JobVacancy>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
    {
      var filter = Builders<JobVacancy>.Filter.Where(x => x.IsDeleted != true);
      var total = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
      var items = await _collection.Find(filter)
          .Skip((pagination.PageNumber - 1) * pagination.PageSize)
          .Limit(pagination.PageSize)
          .ToListAsync(cancellationToken);

      return new PagedResult<JobVacancy>
      {
        Items = items,
        TotalCount = (int)total,
        PageNumber = pagination.PageNumber ?? 1,
        PageSize = pagination.PageSize ?? 20
      };
    }

    public async Task<long> CountActiveAsync(CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(x => x.IsDeleted == false, cancellationToken: cancellationToken);

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
      await _collection.DeleteManyAsync(_ => true, cancellationToken);
    }
  }
}
