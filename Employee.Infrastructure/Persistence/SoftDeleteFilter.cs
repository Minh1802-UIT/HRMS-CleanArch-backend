using Employee.Domain.Entities.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Persistence
{
    /// <summary>
    /// Provides global soft-delete filtering for MongoDB queries.
    /// Ensures that soft-deleted entities (IsDeleted = true) are automatically
    /// excluded from all queries unless explicitly requesting deleted records.
    /// </summary>
    public static class SoftDeleteFilter
    {
        /// <summary>
        /// Gets the standard soft-delete filter that excludes deleted entities.
        /// </summary>
        public static FilterDefinition<T> GetActiveOnlyFilter<T>() where T : BaseEntity
        {
            return Builders<T>.Filter.Eq(x => x.IsDeleted, false);
        }

        /// <summary>
        /// Combines an existing filter with the soft-delete filter using AND logic.
        /// </summary>
        public static FilterDefinition<T> CombineWithSoftDeleteFilter<T>(
            FilterDefinition<T> existingFilter) where T : BaseEntity
        {
            return Builders<T>.Filter.And(existingFilter, GetActiveOnlyFilter<T>());
        }

        /// <summary>
        /// Combines multiple filters with the soft-delete filter using AND logic.
        /// </summary>
        public static FilterDefinition<T> CombineWithSoftDeleteFilter<T>(
            params FilterDefinition<T>[] filters) where T : BaseEntity
        {
            var allFilters = new List<FilterDefinition<T>>(filters)
            {
                GetActiveOnlyFilter<T>()
            };
            return Builders<T>.Filter.And(allFilters);
        }
    }
}
