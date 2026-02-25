using MongoDB.Driver;

namespace Employee.Infrastructure.Persistence
{
    public class MongoContext : IMongoContext
    {
        private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
        public IClientSessionHandle? Session { get; private set; }

        public MongoContext(IMongoClient client, string databaseName)
        {
            _client = client;
      _database = _client.GetDatabase(databaseName);

      // Register Domain Mappings
      MongoMappingConfig.RegisterMappings();
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
      return _database.GetCollection<T>(name);
        }

        public async Task<IClientSessionHandle> StartSessionAsync()
        {
            Session = await _client.StartSessionAsync();
            return Session;
        }
    }
}
