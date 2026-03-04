using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Enums;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Payroll
{
  public class PayrollCycleRepository : BaseRepository<PayrollCycle>, IPayrollCycleRepository
  {
    public PayrollCycleRepository(IMongoContext context) : base(context, "payroll_cycles")
    {
    }

    public async Task<PayrollCycle?> GetByMonthKeyAsync(
        string monthKey, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollCycle>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollCycle>(),
          Builders<PayrollCycle>.Filter.Eq(x => x.MonthKey, monthKey));
      return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PayrollCycle>> GetByYearAsync(
        int year, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollCycle>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollCycle>(),
          Builders<PayrollCycle>.Filter.Eq(x => x.Year, year));
      return await _collection.Find(filter)
          .SortBy(x => x.Month)
          .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string monthKey, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollCycle>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollCycle>(),
          Builders<PayrollCycle>.Filter.Eq(x => x.MonthKey, monthKey));
      return await _collection.Find(filter).AnyAsync(cancellationToken);
    }
  }
}
