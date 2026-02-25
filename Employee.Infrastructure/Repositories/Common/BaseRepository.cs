using System.Threading;
using Employee.Application.Common.Interfaces.Common;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.Common;
using Employee.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Common
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
    {
        protected readonly IMongoCollection<T> _collection;
        protected readonly IMongoContext _context;

        protected BaseRepository(IMongoContext context, string collectionName)
        {
            _context = context;
            _collection = _context.GetCollection<T>(collectionName);
        }

        public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq(x => x.Id, id),
                SoftDeleteFilter.GetActiveOnlyFilter<T>()
            );
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _collection.Find(SoftDeleteFilter.GetActiveOnlyFilter<T>()).ToListAsync(cancellationToken);
        }

        public virtual async Task<PagedResult<T>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
        {
            var filter = SoftDeleteFilter.GetActiveOnlyFilter<T>();
            var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            var query = _collection.Find(filter);

            if (!string.IsNullOrEmpty(pagination.SortBy))
            {
                // Guard against MongoDB field-name injection: allow only identifiers
                // of the form [a-zA-Z][a-zA-Z0-9_]* (no $, dots, spaces, etc.).
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        pagination.SortBy, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                {
                    throw new ArgumentException(
                        $"SortBy value '{pagination.SortBy}' is not a valid field name.",
                        nameof(pagination));
                }

                var sort = pagination.IsDescending.GetValueOrDefault()
                    ? Builders<T>.Sort.Descending(pagination.SortBy)
                    : Builders<T>.Sort.Ascending(pagination.SortBy);
                query = query.Sort(sort);
            }

            var items = await query
                .Skip((pagination.PageNumber.GetValueOrDefault(1) - 1) * pagination.PageSize.GetValueOrDefault(20))
                .Limit(pagination.PageSize.GetValueOrDefault(20))
                .ToListAsync(cancellationToken);

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = (int)totalCount,
                PageNumber = pagination.PageNumber.GetValueOrDefault(1),
                PageSize = pagination.PageSize.GetValueOrDefault(20)
            };
        }

        public virtual async Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (_context.Session != null)
                await _collection.InsertOneAsync(_context.Session, entity, cancellationToken: cancellationToken);
            else
                await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        }

        public virtual async Task UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
        {
            // Default update without version check (legacy/simple)
            entity.SetUpdatedAt(DateTime.UtcNow);
            if (_context.Session != null)
                await _collection.ReplaceOneAsync(_context.Session, x => x.Id == id, entity, cancellationToken: cancellationToken);
            else
                await _collection.ReplaceOneAsync(x => x.Id == id, entity, cancellationToken: cancellationToken);
        }

        public virtual async Task UpdateAsync(string id, T entity, int expectedVersion, CancellationToken cancellationToken = default)
        {
            entity.SetVersion(expectedVersion + 1);
            entity.SetUpdatedAt(DateTime.UtcNow);

            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq(x => x.Id, id),
                Builders<T>.Filter.Eq(x => x.Version, expectedVersion)
            );

            ReplaceOneResult result;
            if (_context.Session != null)
                result = await _collection.ReplaceOneAsync(_context.Session, filter, entity, cancellationToken: cancellationToken);
            else
                result = await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);

            if (result.ModifiedCount == 0)
                throw new Employee.Application.Common.Exceptions.ConcurrencyException("Concurrency conflict occurred. Refresh and try again.");
        }

        /// <summary>
        /// Soft-deletes an entity by marking it as deleted.
        /// The entity is not physically removed from the database, allowing for recovery if needed.
        /// </summary>
        public virtual async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var update = Builders<T>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            if (_context.Session != null)
                await _collection.UpdateOneAsync(_context.Session, x => x.Id == id, update, cancellationToken: cancellationToken);
            else
                await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Soft-deletes an entity by marking it as deleted, with optional tracking of who deleted it.
        /// </summary>
        public virtual async Task DeleteAsync(string id, string? deletedBy = null)
        {
            var update = Builders<T>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            if (deletedBy != null)
            {
                update = update.Set(x => x.UpdatedBy, deletedBy);
            }

            if (_context.Session != null)
                await _collection.UpdateOneAsync(_context.Session, x => x.Id == id, update);
            else
                await _collection.UpdateOneAsync(x => x.Id == id, update);
        }

        public virtual async Task ClearAllAsync(CancellationToken cancellationToken = default)
        {
            await _collection.DeleteManyAsync(_ => true, cancellationToken);
        }
    }
}
