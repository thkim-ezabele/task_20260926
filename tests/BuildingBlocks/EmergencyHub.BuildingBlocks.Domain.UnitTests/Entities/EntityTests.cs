namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Entities;

public sealed class EntityTests
{
    [Fact]
    public void Constructor_WithId_SetsId()
    {
        var id = SampleId.New();

        var entity = new SampleEntity(id);

        entity.Id.Should().Be(id);
    }

    [Fact]
    public void Equals_WithSameTypeAndSameId_ReturnsTrue()
    {
        var id = SampleId.New();
        var left = new SampleEntity(id);
        var right = new SampleEntity(id);

        var result = left.Equals(right);

        result.Should().BeTrue();
        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Equals_WithSameTypeAndDifferentId_ReturnsFalse()
    {
        var left = new SampleEntity(SampleId.New());
        var right = new SampleEntity(SampleId.New());

        var result = left.Equals(right);

        result.Should().BeFalse();
        (left == right).Should().BeFalse();
        (left != right).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentTypeAndSameId_ReturnsFalse()
    {
        var id = SampleId.New();
        var left = new SampleEntity(id);
        object right = new OtherSampleEntity(id);

        var result = left.Equals(right);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var entity = new SampleEntity(SampleId.New());

        var result = entity.Equals(null);

        result.Should().BeFalse();
        (entity == null).Should().BeFalse();
        (null == entity).Should().BeFalse();
        (entity != null).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithBothNull_ReturnsTrue()
    {
        SampleEntity? left = null;
        SampleEntity? right = null;

        var result = left == right;

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNonEntityObject_ReturnsFalse()
    {
        var id = SampleId.New();
        var entity = new SampleEntity(id);

        var result = entity.Equals(id);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDefaultIdOnDifferentInstances_ReturnsFalse()
    {
        var left = new SampleEntity();
        var right = new SampleEntity();

        var result = left.Equals(right);

        result.Should().BeFalse();
        (left == right).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameInstanceHavingDefaultId_ReturnsTrue()
    {
        var entity = new SampleEntity();

        var result = entity.Equals(entity);

        result.Should().BeTrue();
        entity.GetHashCode().Should().Be(entity.GetHashCode());
    }

    [Fact]
    public void Equals_WithDefaultIdAndAssignedId_ReturnsFalse()
    {
        var transient = new SampleEntity();
        var persisted = new SampleEntity(SampleId.New());

        var result = transient.Equals(persisted);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithExplicitEmptyGuidId_TreatsAsDefaultId()
    {
        var left = new SampleEntity(new SampleId(Guid.Empty));
        var right = new SampleEntity(new SampleId(Guid.Empty));

        var result = left.Equals(right);

        result.Should().BeFalse();
    }
}
