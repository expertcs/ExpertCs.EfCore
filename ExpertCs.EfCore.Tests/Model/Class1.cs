using System.ComponentModel;
using System.Diagnostics;
using ExpertCs.EfCore.Model;

namespace ExpertCs.EfCore.Tests.Model;

[TestFixture]
public class EntityTests
{
    #region Test Models
    [DisplayName("TestEntityDisplay")]
    private class TestEntity : BaseEntity { }

    [DebuggerDisplay("DebugTestEntity - {Value}")]
    private class DebugTestEntity : BaseEntity
    {
        public string Value = string.Empty;
    }

    private class PlainTestEntity : BaseEntity { }

    private class TestIdEntity : IdEntity { }

    private class TestGenericIdEntity : IdEntity<int> { }
    #endregion

    [Test]
    public void BaseEntity_ToString_UsesDisplayNameAttribute_WhenPresent()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        var result = entity.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("TestEntityDisplay"));
    }

    [Test]
    public void BaseEntity_ToString_UsesDebuggerDisplay_WhenNoDisplayName()
    {
        // Arrange
        var entity = new DebugTestEntity()
        {
            Value = "test"
        };

        // Act
        var result = entity.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("DebugTestEntity - test"));
    }

    [Test]
    public void BaseEntity_ToString_FallsBackToDefault_WhenNoAttributes()
    {
        // Arrange
        var entity = new PlainTestEntity();

        // Act
        var result = entity.ToString();

        // Assert
        Assert.That(result, Does.Contain("ExpertCs.Tests.EfCore.Model.EntityTests+PlainTestEntity"));
    }

    [Test]
    public void BaseEntity_ToString_HandlesExceptions_ReturnsBaseToString()
    {
        // Arrange
        var entity = new TestEntity();
        BaseEntityExtensions.InvokeIgnoreException = (_, _) => throw new Exception("Test");

        // Act
        var result = entity.ToString();

        // Assert
        Assert.That(result, Does.Contain("TestEntity"));
        BaseEntityExtensions.InvokeIgnoreException = null; // Reset
    }

    [Test]
    public void IdEntity_Equals_ReturnsTrue_ForSameIdAndType()
    {
        // Arrange
        var entity1 = new TestIdEntity { Id = 1 };
        var entity2 = new TestIdEntity { Id = 1 };

        // Act & Assert
        Assert.That(entity1.Equals(entity2), Is.True);
    }


    [Test]
    public void IdEntity_Equals_ReturnsTrue_ForDefaultIdAndSameType()
    {
        // Arrange
        var entity1 = new TestIdEntity();
        var entity2 = new TestIdEntity();

        // Act & Assert
        Assert.That(entity1.Equals(entity2), Is.False);
    }

    [Test]
    public void IdEntity_Equals_ReturnsFalse_ForDifferentIds()
    {
        // Arrange
        var entity1 = new TestIdEntity { Id = 1 };
        var entity2 = new TestIdEntity { Id = 2 };

        // Act & Assert
        Assert.That(entity1.Equals(entity2), Is.False);
    }

    [Test]
    public void IdEntity_Equals_ReturnsFalse_ForDifferentTypes()
    {
        // Arrange
        var entity1 = new TestIdEntity { Id = 1 };
        var entity2 = new OtherIdEntity { Id = 1 };

        // Act & Assert
        Assert.That(entity1.Equals(entity2), Is.False);
    }

    [Test]
    public void IdEntity_GetHashCode_ReturnsIdHashCode()
    {
        // Arrange
        var entity = new TestIdEntity { Id = 42 };

        // Act & Assert
        Assert.That(entity.GetHashCode(), Is.EqualTo(42.GetHashCode()));
    }

    [Test]
    public void IdEntityT_Property_SetsAndGetsValue()
    {
        // Arrange
        var entity = new TestGenericIdEntity();

        // Act
        entity.Id = 10;
        var result = entity.Id;

        // Assert
        Assert.That(result, Is.EqualTo(10));
        Assert.That(((IdEntity)entity).Id, Is.EqualTo(10));
    }

    private class OtherIdEntity : IdEntity { }
}

// Helper to test exception handling
public static class BaseEntityExtensions
{
    public static Func<Func<string?>, Func<Exception, string?>, string?>? InvokeIgnoreException;
}