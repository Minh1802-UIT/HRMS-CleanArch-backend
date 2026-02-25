using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Employee.Infrastructure.Persistence
{
  public class UnitOfWork : IUnitOfWork
  {
    private readonly IMongoContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(IMongoContext context, ILogger<UnitOfWork> logger)
    {
      _context = context;
      _logger = logger;
    }

    public async Task BeginTransactionAsync()
    {
      var session = await _context.StartSessionAsync();
      try
      {
        session.StartTransaction();
      }
      catch (MongoDB.Driver.MongoCommandException ex) when (ex.Code == 20 || ex.Message.Contains("Standalone servers do not support transactions"))
      {
        // Standalone MongoDB (local dev) does not support transactions.
        // Log a warning so it is visible in structured logs and proceed without a transaction.
        // In production (Replica Set / Atlas), this branch should never be reached.
        _logger.LogWarning("UnitOfWork: MongoDB transaction not supported on this server. Proceeding without transaction. Details: {Message}", ex.Message);
      }
      catch (Exception ex)
      {
        // Fallback for generic error message matching if Code isn't 20
        if (ex.Message.Contains("Standalone servers do not support transactions"))
        {
          _logger.LogWarning("UnitOfWork: MongoDB transaction not supported on this server. Proceeding without transaction. Details: {Message}", ex.Message);
        }
        else
        {
          throw;
        }
      }
    }

    public async Task CommitTransactionAsync()
    {
      if (_context.Session != null && _context.Session.IsInTransaction)
      {
        await _context.Session.CommitTransactionAsync();
      }
    }

    public async Task RollbackTransactionAsync()
    {
      if (_context.Session != null && _context.Session.IsInTransaction)
      {
        await _context.Session.AbortTransactionAsync();
      }
    }

    public void Dispose()
    {
      _context.Session?.Dispose();
    }
  }
}
