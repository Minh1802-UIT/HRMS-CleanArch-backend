using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Bson;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/";
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase("HRMS");
        var collection = db.GetCollection<BsonDocument>("contracts");

        var filter = Builders<BsonDocument>.Filter.Eq("EmployeeId", "67c06ebf03f2a5758a4364b0");
        var contracts = await collection.Find(filter).ToListAsync();

        Console.WriteLine($"Found {contracts.Count} contracts for employee 67c06ebf03f2a5758a4364b0:");
        foreach (var contract in contracts)
        {
            Console.WriteLine(contract.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true }));
        }
    }
}
