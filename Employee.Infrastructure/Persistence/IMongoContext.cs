using MongoDB.Driver;

namespace Employee.Infrastructure.Persistence
{
  public interface IMongoContext
  {
    IMongoCollection<T> GetCollection<T>(string name);
    IClientSessionHandle? Session { get; }
    Task<IClientSessionHandle> StartSessionAsync();
  }
}
