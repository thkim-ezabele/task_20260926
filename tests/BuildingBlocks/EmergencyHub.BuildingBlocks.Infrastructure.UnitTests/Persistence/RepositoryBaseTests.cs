using System.Reflection;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04: Repository 기반. 쓰기 기반은 WriteDbContextBase, 읽기 기반은 ReadDbContextBase만 받아 컴파일 시점에 섞이지 않는다.
// 두 기반 모두 protected Db만 노출하고 SaveChanges를 감싸지 않는다(저장은 UnitOfWork, ADR-0014).
[Trait("FR", "PRD-001/FR-06")]
public sealed class RepositoryBaseTests
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // ---- 성공 ----

    [Fact]
    public void RepositoryBase_GivenWriteContext_ExposesItAsDb()
    {
        using var context = SampleDbContexts.CreateWrite();

        var repository = new SampleOrderRepository(context);

        repository.ExposedDb.Should().BeSameAs(context);
    }

    [Fact]
    public void ReadRepositoryBase_GivenReadContext_ExposesItAsDb()
    {
        using var context = SampleDbContexts.CreateRead();

        var repository = new SampleOrderReadRepository(context);

        repository.ExposedDb.Should().BeSameAs(context);
    }

    [Fact]
    public void RepositoryBase_Add_TracksAggregateWithoutSaving()
    {
        using var context = SampleDbContexts.CreateWrite();
        var repository = new SampleOrderRepository(context);
        var order = SampleDbContexts.NewOrder();

        repository.Add(order);

        context.Entry(order).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public void ReadRepositoryBase_Query_TranslatesProjectionAgainstReadContext()
    {
        using var context = SampleDbContexts.CreateRead();
        var repository = new SampleOrderReadRepository(context);

        var sql = repository.OrderNumbers().ToQueryString();

        sql.Should().Contain("SELECT o.order_number").And.Contain("FROM orders");
    }

    // ---- 계약 형태 ----

    [Theory]
    [InlineData(typeof(RepositoryBase<>), typeof(WriteDbContextBase))]
    [InlineData(typeof(ReadRepositoryBase<>), typeof(ReadDbContextBase))]
    public void TypeParameter_IsConstrainedToMatchingContextBase(Type repositoryBase, Type contextBase)
    {
        var constraints = repositoryBase.GetGenericArguments().Single().GetGenericParameterConstraints();

        constraints.Should().Equal(contextBase);
    }

    [Theory]
    [InlineData(typeof(RepositoryBase<>))]
    [InlineData(typeof(ReadRepositoryBase<>))]
    public void Members_AreOnlyProtectedDbAndConstructor_WithoutSaveChanges(Type repositoryBase)
    {
        var members = repositoryBase.GetMembers(DeclaredMembers)
            .Where(member => member is not FieldInfo field || !field.IsPrivate)
            .ToList();

        members.Where(member => member is not ConstructorInfo).Select(member => member.Name)
            .Should().BeEquivalentTo(["Db", "get_Db"]);
        repositoryBase.GetProperty("Db", BindingFlags.NonPublic | BindingFlags.Instance)!.GetMethod!.IsFamily
            .Should().BeTrue("Db는 파생 Repository만 쓴다(protected)");
        repositoryBase.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void ContextBases_AreUnrelatedSoWriteAndReadCannotBeSwapped()
    {
        typeof(WriteDbContextBase).IsAssignableFrom(typeof(ReadDbContextBase)).Should().BeFalse();
        typeof(ReadDbContextBase).IsAssignableFrom(typeof(WriteDbContextBase)).Should().BeFalse();
    }

    // ---- 실패 ----

    [Fact]
    public void RepositoryBase_NullContext_ThrowsArgumentNullException()
    {
        var act = () => new SampleOrderRepository(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadRepositoryBase_NullContext_ThrowsArgumentNullException()
    {
        var act = () => new SampleOrderReadRepository(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
