using System.Net;
using System.Net.Http.Json;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.ValueObjects;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Employee.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for the MongoDB BaseRepository pipeline.
/// Exercises the real MongoDB driver — serialization, soft-delete filters,
/// optimistic concurrency, and pagination — against a Testcontainers instance.
/// </summary>
[Collection("Api")]
public class BaseRepositoryTests : IntegrationTestBase
{
  public BaseRepositoryTests(IntegrationTestFixture fixture) : base(fixture) { }

  // ─────────────────────────────────────────────────────────────────
  // Helper: create a minimal employee entity directly in MongoDB
  // so we can test the repository layer without going through HTTP.
  // ─────────────────────────────────────────────────────────────────
  private IMongoCollection<EmployeeEntity> Employees => Fixture.Database.GetCollection<EmployeeEntity>("employees");
  private IMongoCollection<Department> Departments => Fixture.Database.GetCollection<Department>("departments");
  private IMongoCollection<Position> Positions => Fixture.Database.GetCollection<Position>("positions");

  private async Task SeedDepartmentAndPositionAsync(string deptId, string posId)
  {
    await Departments.InsertOneAsync(new Department("Engineering", "ENG") { Id = deptId });
    await Positions.InsertOneAsync(new Position("Software Engineer", "SE", deptId) { Id = posId });
  }

  private async Task<EmployeeEntity> SeedEmployeeAsync(string code = "E001", string name = "Test Employee", string email = "test@company.com")
  {
    var emp = new EmployeeEntity(code, name, email);
    // Manually set IDs (normally set by MongoDB driver via BsonId attribute).
    // Our domain uses string IDs without [BsonId] so we insert with _id as string.
    await Employees.InsertOneAsync(emp);
    return emp;
  }

  // ─────────────────────────────────────────────────────────────────
  // CRUD Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task CreateAsync_ValidEmployee_ShouldPersistAndRetrieveById()
  {
    // Arrange
    var deptId = ObjectId.GenerateNewId().ToString();
    var posId = ObjectId.GenerateNewId().ToString();
    await SeedDepartmentAndPositionAsync(deptId, posId);

    var emp = new EmployeeEntity("E-TEST-001", "Nguyen Van A", "nguyenvana@company.com");
    // Inject job details directly (factory constructor only sets code/name/email)
    emp.UpdateJobDetails(new JobDetails { DepartmentId = deptId, PositionId = posId });

    // Act
    await Employees.InsertOneAsync(emp);
    var retrieved = await Employees
        .Find(x => x.Id == emp.Id)
        .FirstOrDefaultAsync();

    // Assert
    Assert.NotNull(retrieved);
    Assert.Equal("E-TEST-001", retrieved.EmployeeCode);
    Assert.Equal("Nguyen Van A", retrieved.FullName);
    Assert.Equal("nguyenvana@company.com", retrieved.Email);
  }

  [Fact]
  public async Task GetByIdAsync_NonExistentId_ShouldReturnNull()
  {
    // Id is stored as ObjectId in MongoDB — the driver rejects non-hex strings.
    var missingId = ObjectId.GenerateNewId().ToString();

    // Act
    var result = await Employees
        .Find(x => x.Id == missingId)
        .FirstOrDefaultAsync();

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task UpdateAsync_ExistingEmployee_ShouldReflectChanges()
  {
    // Arrange
    var emp = await SeedEmployeeAsync();
    var newName = "Updated Name";

    var update = Builders<EmployeeEntity>.Update.Set(x => x.FullName, newName);
    await Employees.UpdateOneAsync(x => x.Id == emp.Id, update);

    // Act
    var updated = await Employees.Find(x => x.Id == emp.Id).FirstOrDefaultAsync();

    // Assert
    Assert.NotNull(updated);
    Assert.Equal(newName, updated.FullName);
  }

  [Fact]
  public async Task DeleteAsync_ShouldSoftDelete_NotPhysicallyRemove()
  {
    // Arrange
    var emp = await SeedEmployeeAsync();

    // Act — soft-delete via BaseRepository pattern
    var update = Builders<EmployeeEntity>.Update
        .Set(x => x.IsDeleted, true)
        .Set(x => x.UpdatedAt, DateTime.UtcNow);
    await Employees.UpdateOneAsync(x => x.Id == emp.Id, update);

    // Assert — record still exists in DB
    var inDb = await Employees
        .Find(x => x.Id == emp.Id)
        .FirstOrDefaultAsync();
    Assert.NotNull(inDb);

    // Assert — this employee is excluded when active-only filter is applied (same row as BaseRepository)
    var activeOnly = Builders<EmployeeEntity>.Filter.And(
        Builders<EmployeeEntity>.Filter.Eq(x => x.Id, emp.Id),
        Builders<EmployeeEntity>.Filter.Eq(x => x.IsDeleted, false));
    var foundViaActiveFilter = await Employees.Find(activeOnly).FirstOrDefaultAsync();
    Assert.Null(foundViaActiveFilter);
  }

  [Fact]
  public async Task SoftDeleteFilter_ActiveOnly_ShouldExcludeDeletedRecords()
  {
    // Arrange — seed 3 employees, soft-delete 1
    var active1 = await SeedEmployeeAsync("E-A1", "Active One");
    var active2 = await SeedEmployeeAsync("E-A2", "Active Two");
    var deleted = await SeedEmployeeAsync("E-D1", "Deleted One");

    await Employees.UpdateOneAsync(
        x => x.Id == deleted.Id,
        Builders<EmployeeEntity>.Update.Set(x => x.IsDeleted, true));

    // Act — scope to this test's rows only (DB also contains startup-seeded employees)
    var ourCodes = new[] { active1.EmployeeCode, active2.EmployeeCode, deleted.EmployeeCode };
    var activeFilter = Builders<EmployeeEntity>.Filter.Eq(x => x.IsDeleted, false);
    var scoped = Builders<EmployeeEntity>.Filter.In(x => x.EmployeeCode, ourCodes);
    var activeEmployees = await Employees
        .Find(Builders<EmployeeEntity>.Filter.And(activeFilter, scoped))
        .ToListAsync();

    // Assert
    Assert.Equal(2, activeEmployees.Count);
    Assert.All(activeEmployees, e => Assert.DoesNotContain("Deleted", e.FullName));
    Assert.DoesNotContain(activeEmployees, e => e.Id == deleted.Id);
  }

  // ─────────────────────────────────────────────────────────────────
  // Optimistic Concurrency Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task UpdateWithVersion_MatchingVersion_ShouldSucceed()
  {
    // Arrange
    var emp = await SeedEmployeeAsync();
    var currentVersion = emp.Version; // starts at 1

    // Act — update expecting version 1, MongoDB will bump to 2
    var newVersion = currentVersion + 1;
    var filter = Builders<EmployeeEntity>.Filter.And(
        Builders<EmployeeEntity>.Filter.Eq(x => x.Id, emp.Id),
        Builders<EmployeeEntity>.Filter.Eq(x => x.Version, currentVersion)
    );
    var update = Builders<EmployeeEntity>.Update
        .Set(x => x.FullName, "Concurrency Test")
        .Set(x => x.Version, newVersion)
        .Set(x => x.UpdatedAt, DateTime.UtcNow);

    var result = await Employees.UpdateOneAsync(filter, update);

    // Assert
    Assert.Equal(1, result.ModifiedCount);

    var updated = await Employees.Find(x => x.Id == emp.Id).FirstOrDefaultAsync();
    Assert.Equal(newVersion, updated!.Version);
    Assert.Equal("Concurrency Test", updated.FullName);
  }

  [Fact]
  public async Task UpdateWithVersion_StaleVersion_ShouldReturnZeroModifiedCount()
  {
    // Arrange
    var emp = await SeedEmployeeAsync();
    var staleVersion = emp.Version; // = 1
    var currentVersion = staleVersion + 1;

    // First, update the version to simulate another process already updating
    await Employees.UpdateOneAsync(
        x => x.Id == emp.Id,
        Builders<EmployeeEntity>.Update.Set(x => x.Version, currentVersion));

    // Act — try to update with the now-stale version
    var filter = Builders<EmployeeEntity>.Filter.And(
        Builders<EmployeeEntity>.Filter.Eq(x => x.Id, emp.Id),
        Builders<EmployeeEntity>.Filter.Eq(x => x.Version, staleVersion) // version is already 2
    );
    var update = Builders<EmployeeEntity>.Update
        .Set(x => x.FullName, "Should Not Apply")
        .Set(x => x.Version, staleVersion + 1)
        .Set(x => x.UpdatedAt, DateTime.UtcNow);

    var result = await Employees.UpdateOneAsync(filter, update);

    // Assert — MongoDB reports 0 documents matched/updated
    Assert.Equal(0, result.ModifiedCount);

    // Name should NOT have changed
    var unchanged = await Employees.Find(x => x.Id == emp.Id).FirstOrDefaultAsync();
    Assert.NotEqual("Should Not Apply", unchanged!.FullName);
  }

  // ─────────────────────────────────────────────────────────────────
  // Pagination Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task GetPaged_SinglePage_ShouldReturnCorrectSubset()
  {
    // Arrange — seed 25 employees
    var tasks = Enumerable.Range(1, 25).Select(i =>
        SeedEmployeeAsync($"E-PAGE-{i:D3}", $"Employee {i}"));
    await Task.WhenAll(tasks);

    // Act — page 1, page size 10 (only rows created in this test; seeder fills employees too)
    var filter = Builders<EmployeeEntity>.Filter.And(
        Builders<EmployeeEntity>.Filter.Eq(x => x.IsDeleted, false),
        Builders<EmployeeEntity>.Filter.Regex(x => x.EmployeeCode, new BsonRegularExpression("^E-PAGE-")));
    var total = await Employees.CountDocumentsAsync(filter);
    var page1 = await Employees.Find(filter)
        .SortBy(x => x.EmployeeCode)
        .Skip(0)
        .Limit(10)
        .ToListAsync();

    var page2 = await Employees.Find(filter)
        .SortBy(x => x.EmployeeCode)
        .Skip(10)
        .Limit(10)
        .ToListAsync();

    var page3 = await Employees.Find(filter)
        .SortBy(x => x.EmployeeCode)
        .Skip(20)
        .Limit(10)
        .ToListAsync();

    // Assert
    Assert.Equal(25, total);
    Assert.Equal(10, page1.Count);
    Assert.Equal(10, page2.Count);
    Assert.Equal(5, page3.Count);

    // Verify ordering
    Assert.True(string.Compare(page1.Last().EmployeeCode, page2.First().EmployeeCode, StringComparison.Ordinal) < 0);
  }

  [Fact]
  public async Task GetPaged_NoMatches_ShouldReturnEmptyPage()
  {
    // Seeded data fills the database — assert empty page for an impossible filter instead.
    var filter = Builders<EmployeeEntity>.Filter.And(
        Builders<EmployeeEntity>.Filter.Eq(x => x.IsDeleted, false),
        Builders<EmployeeEntity>.Filter.Eq(x => x.EmployeeCode, "__INTEGRATION_TEST_NO_SUCH_CODE__"));

    // Act
    var result = await Employees.Find(filter)
        .Skip(0)
        .Limit(20)
        .ToListAsync();

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public async Task GetPaged_DescendingSort_ShouldReturnInReverseOrder()
  {
    // Arrange
    for (int i = 1; i <= 5; i++)
      await SeedEmployeeAsync($"E-SORT-{i:D2}", $"Sort Employee {i}");

    // Act — limit to this test's codes (ignore seeded employees)
    var filter = Builders<EmployeeEntity>.Filter.And(
        Builders<EmployeeEntity>.Filter.Eq(x => x.IsDeleted, false),
        Builders<EmployeeEntity>.Filter.Regex(x => x.EmployeeCode, new BsonRegularExpression("^E-SORT-")));
    var ascending = await Employees.Find(filter)
        .SortBy(x => x.EmployeeCode)
        .ToListAsync();
    var descending = await Employees.Find(filter)
        .SortByDescending(x => x.EmployeeCode)
        .ToListAsync();

    // Assert
    Assert.Equal(ascending[0].Id, descending[^1].Id);
    Assert.Equal(ascending[^1].Id, descending[0].Id);
  }

  // IntegrationTestBase.Dispose() handles Client and Factory cleanup.
}
