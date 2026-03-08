using Employee.Infrastructure.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Employee.Infrastructure.Persistence;

namespace Employee.UnitTests.Infrastructure.BackgroundServices;

public class SoftDeleteCleanupBackgroundServiceTests
{
    private readonly Mock<IMongoContext> _mockContext;
    private readonly Mock<ILogger<SoftDeleteCleanupBackgroundService>> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<Mock<IMongoCollection<BsonDocument>>> _mockCollections;

    public SoftDeleteCleanupBackgroundServiceTests()
    {
        _mockContext = new Mock<IMongoContext>();
        _logger = new Mock<ILogger<SoftDeleteCleanupBackgroundService>>();
        _mockCollections = new List<Mock<IMongoCollection<BsonDocument>>>();

        var services = new ServiceCollection();
        services.AddScoped(_ => _mockContext.Object);
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task RunCleanupAsync_DeletesSoftDeletedRecords_OlderThanRetentionDays()
    {
        // Arrange
        var collectionName = "employees";
        var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
        _mockCollections.Add(mockCollection);
        
        var cutoff = DateTime.UtcNow.AddDays(-90);
        
        // Setup successful delete
        var deleteResult = new DeleteResult.Acknowledged(5);
        mockCollection
            .Setup(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult);

        _mockContext.Setup(c => c.GetCollection<BsonDocument>(collectionName)).Returns(mockCollection.Object);

        var service = new SoftDeleteCleanupBackgroundService(_scopeFactory, _logger.Object);

        // Use reflection to call private method
        var method = typeof(SoftDeleteCleanupBackgroundService).GetMethod("RunCleanupAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        await (Task)method!.Invoke(service, new object[] { CancellationToken.None })!;

        // Assert
        mockCollection.Verify(
            c => c.DeleteManyAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunCleanupAsync_HandlesCollectionErrors_ContinuesToNextCollection()
    {
        // Arrange
        var employeesCollection = new Mock<IMongoCollection<BsonDocument>>();
        var contractsCollection = new Mock<IMongoCollection<BsonDocument>>();
        _mockCollections.Add(employeesCollection);
        _mockCollections.Add(contractsCollection);
        
        // First collection throws
        employeesCollection
            .Setup(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Collection not found"));
        
        // Second collection succeeds
        contractsCollection
            .Setup(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(3));

        _mockContext.Setup(c => c.GetCollection<BsonDocument>("employees")).Returns(employeesCollection.Object);
        _mockContext.Setup(c => c.GetCollection<BsonDocument>("contracts")).Returns(contractsCollection.Object);

        var service = new SoftDeleteCleanupBackgroundService(_scopeFactory, _logger.Object);
        
        var method = typeof(SoftDeleteCleanupBackgroundService).GetMethod("RunCleanupAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act & Assert - should not throw, should continue
        await (Task)method!.Invoke(service, new object[] { CancellationToken.None })!;

        // Verify both collections were attempted
        employeesCollection.Verify(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        contractsCollection.Verify(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void TargetCollections_ContainsExpectedCollections()
    {
        // This test verifies the configuration of collections that should be cleaned
        
        var expectedCollections = new[] 
        { 
            "employees", 
            "contracts", 
            "leave_requests", 
            "leave_allocations",
            "attendance_buckets",
            "payrolls",
            "shifts"
        };

        // Get the private static field
        var field = typeof(SoftDeleteCleanupBackgroundService).GetField("TargetCollections", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var collections = (string[])field!.GetValue(null)!;
        
        Assert.Equal(expectedCollections.Length, collections.Length);
        foreach (var expected in expectedCollections)
        {
            Assert.Contains(expected, collections);
        }
    }

    [Fact]
    public void RetentionDays_IsNinetyDays()
    {
        var field = typeof(SoftDeleteCleanupBackgroundService).GetField("RetentionDays", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var retentionDays = (int)field!.GetValue(null)!;
        
        Assert.Equal(90, retentionDays);
    }
}
