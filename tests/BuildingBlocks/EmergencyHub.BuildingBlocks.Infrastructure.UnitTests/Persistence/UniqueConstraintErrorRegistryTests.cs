using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// 원본: database.md "23505 매핑 레지스트리 계약". 키 UniqueIndexName(서비스 상수), 값 Error는 Conflict만, 같은 키 두 번이면 예외,
// 등록 뒤 불변, 조회는 ConstraintName 문자열과 UniqueIndexName.Value의 Ordinal 비교(조회 때 UniqueIndexName을 만들지 않음).
[Trait("FR", "PRD-001/FR-06")]
public sealed class UniqueConstraintErrorRegistryTests
{
    private static readonly UniqueIndexName OtherIndex = new("ux_orders_customer_email");
    private static readonly Error OtherConflict = Error.Conflict(23002, "이미 등록된 고객 이메일입니다.");

    // ---- 성공 ----

    [Fact]
    public void Find_MappedConstraintName_ReturnsMappedError()
    {
        var registry = SampleUniqueConstraintErrors.Create();

        registry.Find(SampleIndexNames.OrdersOrderNumber.Value).Should().Be(SampleErrors.ValueConflict);
    }

    [Fact]
    public void Map_TwoDifferentIndexes_BothFoundAndListed()
    {
        var registry = new UniqueConstraintErrorsBuilder()
            .Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueConflict)
            .Map(OtherIndex, OtherConflict)
            .Build();

        registry.Find(OtherIndex.Value).Should().Be(OtherConflict);
        registry.IndexNames.Should().BeEquivalentTo([SampleIndexNames.OrdersOrderNumber, OtherIndex]);
    }

    [Fact]
    public void Map_CommonUniqueConstraintViolatedAsServiceError_IsAllowedBecauseItIsConflict()
    {
        var act = () => new UniqueConstraintErrorsBuilder().Map(OtherIndex, CommonErrors.UniqueConstraintViolated);

        act.Should().NotThrow();
    }

    [Fact]
    public void IndexNames_OfSampleRegistry_AllExistAsUniqueIndexNamesInModel()
    {
        // 서비스 테스트가 할 단언의 예: 레지스트리 키가 모델의 유니크 인덱스 이름(GetDatabaseName())에 있어야 이름 변경 · 오타가 드러난다.
        using var context = SampleDbContexts.CreateWrite();
        var modelIndexNames = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetIndexes())
            .Where(index => index.IsUnique)
            .Select(index => index.GetDatabaseName())
            .ToList();

        var registry = SampleUniqueConstraintErrors.Create();

        registry.IndexNames.Should().NotBeEmpty();
        registry.IndexNames.Select(name => name.Value).Should().BeSubsetOf(modelIndexNames);
    }

    // ---- 실패: 등록 규칙 ----

    [Theory]
    [MemberData(nameof(NonConflictErrors))]
    public void Map_NonConflictError_ThrowsArgumentException(Error nonConflict)
    {
        var act = () => new UniqueConstraintErrorsBuilder().Map(SampleIndexNames.OrdersOrderNumber, nonConflict);

        act.Should().Throw<ArgumentException>().WithParameterName("error");
    }

    [Fact]
    public void Map_SameIndexTwiceWithSameError_ThrowsInvalidOperationException()
    {
        var builder = new UniqueConstraintErrorsBuilder().Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueConflict);

        var act = () => builder.Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueConflict);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{SampleIndexNames.OrdersOrderNumber.Value}*");
    }

    [Fact]
    public void Map_EqualIndexNameFromAnotherInstance_ThrowsInvalidOperationExceptionBecauseKeyIsValueEqual()
    {
        var builder = new UniqueConstraintErrorsBuilder().Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueConflict);

        var act = () => builder.Map(new UniqueIndexName(SampleIndexNames.OrdersOrderNumber.Value), OtherConflict);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Map_NullIndexName_ThrowsArgumentNullException()
    {
        var act = () => new UniqueConstraintErrorsBuilder().Map(null!, SampleErrors.ValueConflict);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_NullError_ThrowsArgumentNullException()
    {
        var act = () => new UniqueConstraintErrorsBuilder().Map(SampleIndexNames.OrdersOrderNumber, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ---- 엣지: 조회 ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pk_orders")]
    [InlineData("UX_ORDERS_ORDER_NUMBER")]
    [InlineData(" ux_orders_order_number")]
    [InlineData("ux_orders_order_number_extra")]
    public void Find_UnmappedOrInvalidConstraintName_ReturnsNullWithoutThrowing(string? constraintName)
    {
        var registry = SampleUniqueConstraintErrors.Create();

        var act = () => registry.Find(constraintName);

        act.Should().NotThrow().Which.Should().BeNull();
    }

    [Fact]
    public void Build_NoMappings_IsEmptyAndFindsNothing()
    {
        var registry = new UniqueConstraintErrorsBuilder().Build();

        registry.IndexNames.Should().BeEmpty();
        registry.Find(SampleIndexNames.OrdersOrderNumber.Value).Should().BeNull();
    }

    [Fact]
    public void Build_ThenMapMore_BuiltRegistryDoesNotChange()
    {
        var builder = new UniqueConstraintErrorsBuilder().Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueConflict);
        var registry = builder.Build();

        builder.Map(OtherIndex, OtherConflict);

        registry.Find(OtherIndex.Value).Should().BeNull();
        registry.IndexNames.Should().ContainSingle();
    }

    public static TheoryData<Error> NonConflictErrors() =>
    [
        SampleErrors.ValueNegative,
        CommonErrors.NotFound,
        CommonErrors.Unexpected,
        CommonErrors.TemporarilyUnavailable,
        Error.BusinessRule(24001, "업무 규칙 위반"),
    ];
}
