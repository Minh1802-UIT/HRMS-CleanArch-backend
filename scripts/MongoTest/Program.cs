using System;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongoTest
{
    public record SalaryComponents
    {
        public decimal BasicSalary { get; init; }
        public decimal TransportAllowance { get; init; }
        public decimal LunchAllowance { get; init; }
        public decimal OtherAllowance { get; init; }
    }

    [BsonIgnoreExtraElements]
    public class ContractEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? EmployeeId { get; set; }
        public string? Status { get; set; }
        public SalaryComponents? Salary { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var client = new MongoClient("mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/");
                var db = client.GetDatabase("EmployeeCleanDB");
                var coll = db.GetCollection<ContractEntity>("contracts");

                var filter = Builders<ContractEntity>.Filter.Eq(c => c.Status, "Active");
                var projection = Builders<ContractEntity>.Projection
                    .Include(c => c.EmployeeId)
                    .Include(c => c.Status)
                    .Include("Salary.BasicSalary")
                    .Include("Salary.TransportAllowance")
                    .Include("Salary.LunchAllowance")
                    .Include("Salary.OtherAllowance");

                var results = coll.Find(filter).Project<ContractEntity>(projection).ToList();

                using var writer = new StreamWriter("out.log");
                foreach (var c in results)
                {
                    writer.WriteLine($"EmployeeId: {c.EmployeeId}, BasicSalary: {(c.Salary != null ? c.Salary.BasicSalary : "Salary is NULL")}");
                }
                writer.WriteLine("SUCCESS");
            }
            catch (Exception ex)
            {
                File.WriteAllText("error.txt", ex.ToString());
            }
        }
    }
}
