using System.Linq.Expressions;
using ExpertCs.EfCore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ExpertCs.EfCore.Tests.Utils;

[TestFixture]
public class DbContextExtensionsTests
{
    private TestDbContext _dbContext;
    private Mock<ILogger> _loggerMock;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _loggerMock = new Mock<ILogger>();

        DbContextExtensions.LogLevel = LogLevel.Debug;
        DbContextExtensions.CheckFound = false;
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    #region Test Models
    public class TestEntity : IdEntity<int>
    {
        public string Name { get; set; } = default!;
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }
        public DbSet<TestEntity> TestEntities { get; set; }
    }
    #endregion

    [Test]
    public void GetQuery_WithTracking_ReturnsTrackedQuery()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        _dbContext.Add(entity);
        _dbContext.SaveChanges();

        // Act
        var query = _dbContext.GetQuery<TestEntity>(tracking: QueryTrackingBehavior.TrackAll);

        // Assert        
        Assert.That(_dbContext.Entry(query.First()).State, Is.EqualTo(EntityState.Unchanged));
    }

    [Test]
    public async Task GetById_ReturnsEntity_WhenExists()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _dbContext.GetById<TestEntity>(1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Test"));
    }

    [Test]
    public async Task GetById_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _dbContext.GetById<TestEntity>(999, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetById_ThrowsNotFound_WhenCheckFoundEnabledAndNotExists()
    {
        // Arrange
        DbContextExtensions.CheckFound = true;

        // Act & Assert
        Assert.ThrowsAsync<NotFoundExcetion>(() =>
            _dbContext.GetById<TestEntity>(999, CancellationToken.None));
    }

    [Test]
    public async Task AddItem_AddsEntity_AndReturnsIt()
    {
        // Arrange
        var entity = new TestEntity { Id = 3, Name = "Test" };

        // Act
        var result = await _dbContext.AddItem(entity, CancellationToken.None, _loggerMock.Object);

        // Assert
        Assert.That(result, Is.EqualTo(entity));
        Assert.That(_dbContext.TestEntities.Count(), Is.EqualTo(1));
        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AddItem")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }

    [Test]
    public async Task UpdateItem_UpdatesEntity_AndReturnsIt()
    {
        // Arrange
        var entity = new TestEntity { Id = 4, Name = "Original" };
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.Entry(entity).State = EntityState.Detached;

        var updatedEntity = new TestEntity { Id = 4, Name = "Updated" };

        // Act
        var result = await _dbContext.UpdateItem(updatedEntity, CancellationToken.None, _loggerMock.Object);

        // Assert
        Assert.That(result!.Name, Is.EqualTo("Updated"));
        Assert.That(_dbContext.TestEntities.First().Name, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task DeleteItem_DeletesEntity_AndReturns1()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "ToDelete" };
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.Entry(entity).State = EntityState.Detached;

        // Act
        var result = await _dbContext.DeleteItem<TestEntity>(1, CancellationToken.None, _loggerMock.Object);

        // Assert
        Assert.That(result, Is.EqualTo(1));
        Assert.That(_dbContext.TestEntities.Count(), Is.EqualTo(0));
    }

    [Test]
    [Ignore("Not supported by InMemoryDatabase provider")]
    public async Task DeleteItemsExecute_DeletesMatchingEntities()
    {
        // Arrange
        _dbContext.AddRange(
            new TestEntity { Id = 1, Name = "A" },
            new TestEntity { Id = 2, Name = "B" },
            new TestEntity { Id = 3, Name = "A" });
        await _dbContext.SaveChangesAsync();

        Expression<Func<TestEntity, bool>> predicate = x => x.Name == "A";

        // Act
        var result = await _dbContext.DeleteItemsExecute(predicate, CancellationToken.None, _loggerMock.Object);

        // Assert
        Assert.That(result, Is.EqualTo(2));
        Assert.That(_dbContext.TestEntities.Count(), Is.EqualTo(1));
    }

    [Test]
    [Ignore("Not supported by InMemoryDatabase provider")]
    public async Task DeleteItemExecute_DeletesById()
    {
        // Arrange
        _dbContext.AddRange(
            new TestEntity { Id = 1, Name = "A" },
            new TestEntity { Id = 2, Name = "B" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _dbContext.DeleteItemExecute<TestEntity>(1, CancellationToken.None, _loggerMock.Object);

        // Assert
        Assert.That(result, Is.EqualTo(1));
        Assert.That(_dbContext.TestEntities.Count(), Is.EqualTo(1));
    }
}
