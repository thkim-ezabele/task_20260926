using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// 원본: database.md "영속성 예외 변환" 규칙표 1~9(위에서부터 처음 맞는 행). 변환기는 순수 함수라 DB · DI 없이 PostgresException public 생성자로 확인한다.
// 규칙마다 성공(변환) · 실패(변환하지 않음) · 엣지를 둔다. UnitOfWork 경로(실행 전략 바깥에서 잡기 · 로그)는 UnitOfWorkTests.
[Trait("FR", "PRD-001/FR-06")]
public sealed class PersistenceExceptionTranslatorTests : IDisposable
{
    private readonly UniqueConstraintErrorRegistry _registry = SampleUniqueConstraintErrors.Create();
    private readonly SampleWriteDbContext _context = SampleDbContexts.CreateWrite();

    public void Dispose() => _context.Dispose();

    // ---- 규칙 1: DbUpdateConcurrencyException → 3001 ----

    [Fact]
    public void Translate_ConcurrencyException_Returns3001WithEntityTypeAndNoConstraintOrSqlState()
    {
        var entry = _context.Entry(SampleDbContexts.NewOrder());
        entry.State = EntityState.Modified;

        var translation = Translate(SamplePostgresExceptions.Concurrency(entry));

        translation.Should().NotBeNull();
        translation!.Error.Should().Be(CommonErrors.ConcurrencyConflict);
        translation.Kind.Should().Be(PersistenceFailureKind.ConcurrencyConflict);
        translation.ConstraintName.Should().BeNull();
        translation.SqlState.Should().BeNull();
        translation.EntityTypeNames.Should().Equal(nameof(Order));
    }

    [Fact]
    public void Translate_ConcurrencyExceptionWithoutEntries_Returns3001WithNoEntityTypes()
    {
        var translation = Translate(new DbUpdateConcurrencyException("no entries"));

        translation!.Error.Should().Be(CommonErrors.ConcurrencyConflict);
        translation.EntityTypeNames.Should().BeEmpty();
    }

    [Fact]
    public void Translate_ConcurrencyExceptionWrappingUniqueViolation_StillReturns3001BecauseRule1ComesFirst()
    {
        var exception = new DbUpdateConcurrencyException(
            "concurrency",
            SamplePostgresExceptions.Create(PostgresErrorCodes.UniqueViolation, SampleIndexNames.OrdersOrderNumber.Value));

        Translate(exception)!.Error.Should().Be(CommonErrors.ConcurrencyConflict);
    }

    // ---- 규칙 2: 23505 + 레지스트리에 있는 제약 이름 → 서비스 Error ----

    [Fact]
    public void Translate_UniqueViolationOnMappedIndex_ReturnsServiceErrorWithConstraintSqlStateAndEntityType()
    {
        var entry = _context.Entry(SampleDbContexts.NewOrder());
        entry.State = EntityState.Added;

        var translation = Translate(SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value, entry));

        translation!.Error.Should().Be(SampleErrors.ValueConflict);
        translation.Kind.Should().Be(PersistenceFailureKind.MappedUniqueViolation);
        translation.ConstraintName.Should().Be(SampleIndexNames.OrdersOrderNumber.Value);
        translation.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        translation.EntityTypeNames.Should().Equal(nameof(Order));
    }

    [Fact]
    public void Translate_UniqueViolationWithSameEntityTypeTwice_ListsEntityTypeOnce()
    {
        var first = _context.Entry(SampleDbContexts.NewOrder("ORD-1"));
        var second = _context.Entry(SampleDbContexts.NewOrder("ORD-2"));
        first.State = EntityState.Added;
        second.State = EntityState.Added;

        var translation = Translate(SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value, first, second));

        translation!.EntityTypeNames.Should().Equal(nameof(Order));
    }

    [Fact]
    public void Translate_UniqueViolation_ResultAndTranslationNeverContainDetailOrMessageText()
    {
        var translation = Translate(SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value));

        // 메시지는 레지스트리에 정의된 고정 문구이고, 변환 결과에는 Detail · MessageText · 값이 없다.
        translation!.Error.Message.Should().Be(SampleErrors.ValueConflict.Message);
        translation.ToString().Should().NotContain(SamplePostgresExceptions.SecretValue);
        translation.Error.Message.Should().NotContain(SamplePostgresExceptions.SecretValue);
    }

    // ---- 규칙 3: 23505 + 레지스트리에 없는 제약 이름 → 3003 ----

    [Theory]
    [InlineData("ux_orders_customer_email")]
    [InlineData("pk_orders")]
    [InlineData("UX_ORDERS_ORDER_NUMBER")]
    [InlineData("ux_orders_order_number ")]
    [InlineData("")]
    [InlineData(null)]
    public void Translate_UniqueViolationOnUnmappedConstraint_Returns3003(string? constraintName)
    {
        // pk_ · null · 빈 문자열 · 대소문자 · 공백 차이 모두 매핑 없음(Ordinal 비교, 조회 시 UniqueIndexName을 만들지 않으므로 예외 없음).
        var translation = Translate(SamplePostgresExceptions.UniqueViolation(constraintName));

        translation!.Error.Should().Be(CommonErrors.UniqueConstraintViolated);
        translation.Kind.Should().Be(PersistenceFailureKind.UnmappedUniqueViolation);
        translation.ConstraintName.Should().Be(constraintName);
        translation.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public void Translate_UniqueViolationWithEmptyRegistry_Returns3003()
    {
        var empty = new UniqueConstraintErrorsBuilder().Build();

        var translation = PersistenceExceptionTranslator.Translate(
            SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value),
            empty);

        translation!.Error.Should().Be(CommonErrors.UniqueConstraintViolated);
    }

    [Fact]
    public void Translate_UnmappedUniqueViolation_ResultMessageIsFixedAndHasNoDetail()
    {
        var translation = Translate(SamplePostgresExceptions.UniqueViolation("pk_orders"));

        translation!.Error.Message.Should().Be(CommonErrors.UniqueConstraintViolated.Message);
        translation.ToString().Should().NotContain(SamplePostgresExceptions.SecretValue);
    }

    // ---- 규칙 4 · 5: 23505가 아닌 SqlState → 변환하지 않음 ----

    [Theory]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.NotNullViolation)]
    [InlineData(PostgresErrorCodes.StringDataRightTruncation)]
    [InlineData(PostgresErrorCodes.ReadOnlySqlTransaction)]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    public void Translate_DbUpdateExceptionWithOtherSqlState_ReturnsNull(string sqlState)
    {
        var exception = SamplePostgresExceptions.Wrap(SamplePostgresExceptions.Create(sqlState, SampleIndexNames.OrdersOrderNumber.Value));

        Translate(exception).Should().BeNull();
    }

    // ---- 규칙 6: 안쪽이 PostgresException이 아님 · 한 단계 더 감쌈 → 변환하지 않음 ----

    [Fact]
    public void Translate_DbUpdateExceptionWithoutInnerException_ReturnsNull()
    {
        Translate(SamplePostgresExceptions.Wrap(null)).Should().BeNull();
    }

    [Fact]
    public void Translate_DbUpdateExceptionWithNpgsqlExceptionInner_ReturnsNull()
    {
        Translate(SamplePostgresExceptions.Wrap(new NpgsqlException("connection broken"))).Should().BeNull();
    }

    [Fact]
    public void Translate_UniqueViolationWrappedTwoLevelsDeep_ReturnsNullBecauseOnlyDirectInnerIsInspected()
    {
        var twoLevels = SamplePostgresExceptions.Wrap(
            new InvalidOperationException(
                "wrapper",
                SamplePostgresExceptions.Create(PostgresErrorCodes.UniqueViolation, SampleIndexNames.OrdersOrderNumber.Value)));

        Translate(twoLevels).Should().BeNull();
    }

    [Fact]
    public void Translate_DbUpdateExceptionWrappingDbUpdateExceptionWithUniqueViolation_ReturnsNull()
    {
        var nested = SamplePostgresExceptions.Wrap(SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value));

        Translate(nested).Should().BeNull();
    }

    // ---- 규칙 7: 감싸지 않은 PostgresException(COMMIT 등) → 변환하지 않음 ----

    [Fact]
    public void Translate_UnwrappedPostgresUniqueViolation_ReturnsNull()
    {
        var unwrapped = SamplePostgresExceptions.Create(PostgresErrorCodes.UniqueViolation, SampleIndexNames.OrdersOrderNumber.Value);

        Translate(unwrapped).Should().BeNull();
    }

    // ---- 규칙 8 · 9: 재시도 한도 초과 · 취소 → 변환하지 않음 ----

    [Fact]
    public void Translate_RetryLimitExceededWrappingUniqueViolation_ReturnsNullBecauseClassifierHandlesIt()
    {
        var exception = new RetryLimitExceededException(
            "retry limit",
            SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value));

        Translate(exception).Should().BeNull();
    }

    [Fact]
    public void Translate_OperationCanceledException_ReturnsNull()
    {
        Translate(new OperationCanceledException()).Should().BeNull();
    }

    [Fact]
    public void Translate_UnrelatedException_ReturnsNull()
    {
        Translate(new InvalidOperationException("unrelated")).Should().BeNull();
    }

    // ---- 인자 ----

    [Fact]
    public void Translate_NullException_ThrowsArgumentNullException()
    {
        var act = () => PersistenceExceptionTranslator.Translate(null!, _registry);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Translate_NullRegistry_ThrowsArgumentNullException()
    {
        var act = () => PersistenceExceptionTranslator.Translate(new InvalidOperationException(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private PersistenceExceptionTranslation? Translate(Exception exception) =>
        PersistenceExceptionTranslator.Translate(exception, _registry);
}
