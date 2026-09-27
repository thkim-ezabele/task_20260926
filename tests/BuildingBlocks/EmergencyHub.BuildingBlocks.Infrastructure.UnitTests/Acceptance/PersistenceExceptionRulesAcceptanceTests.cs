using System.Data;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Testing;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Acceptance;

// S02-T07 tester: database.md "영속성 예외 변환" 규칙표 1~9를 행마다 UnitOfWork 경로(실제 Npgsql 재시도 실행 전략 + FakeDatabase 대역)로 대조한다.
// 번역기 단위 테스트(PersistenceExceptionTranslatorTests)는 순수 함수만 본다. 여기서는 같은 입력이 전략 바깥에서 잡혀
// 문서의 결과(실패 Result 코드 또는 같은 예외 재전파) · 로그(201 · 202 · 203 또는 없음) · 재시도 여부 · 실패 뒤 상태 유지로 끝나는지를 본다.
// 실제 DB 23505 · xmin 충돌 · 롤백 · 재시도 한도 초과(지연 포함)는 S03-T05 통합 테스트(Testcontainers)가 확인한다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class PersistenceExceptionRulesAcceptanceTests : IDisposable
{
    private static readonly string BeginStep = $"Begin:{IsolationLevel.ReadCommitted}";

    private readonly FakeDatabase _database = new();
    private readonly FakeLogger<UnitOfWork<SampleWriteDbContext>> _logger = new();
    private readonly SampleWriteDbContext _context;
    private readonly RecordingPreCommitHook _hook;
    private readonly UnitOfWork<SampleWriteDbContext> _unitOfWork;

    public PersistenceExceptionRulesAcceptanceTests()
    {
        _context = SampleDbContexts.CreateWrite(_database);
        _hook = new RecordingPreCommitHook(_database);
        _unitOfWork = new UnitOfWork<SampleWriteDbContext>(_context, SampleUniqueConstraintErrors.Create(), [_hook], _logger);
    }

    public void Dispose() => _context.Dispose();

    // ---- 전제: 대역 경로가 실제 재시도 실행 전략을 쓴다 ----

    [Fact]
    public void SampleWriteContext_ExecutionStrategy_IsRealNpgsqlRetryingStrategy()
    {
        // FakeDatabase는 연결 · 트랜잭션 · 저장 · 커밋만 가로챈다. 재시도 판단은 공통 옵션(UseBuildingBlocksNpgsql)의 실제 전략이 한다.
        _context.Database.CreateExecutionStrategy().Should().BeOfType<NpgsqlRetryingExecutionStrategy>();
    }

    // ---- 규칙 1 ~ 3: 변환 → 실패 Result + 로그, 재시도 없음, 커밋 없음, 상태 유지 ----

    [Theory]
    [InlineData(1, "concurrency", 3001, 203)]
    [InlineData(2, "mapped ux_", 23001, 201)]
    [InlineData(3, "pk_", 3003, 202)]
    [InlineData(3, "null", 3003, 202)]
    [InlineData(3, "empty", 3003, 202)]
    [InlineData(3, "unmapped ux_", 3003, 202)]
    public async Task CommitAsync_TranslatedRuleRow_ReturnsDocumentedCodeAndLogWithoutRetryOrCommit(
        int row,
        string variant,
        int expectedCode,
        int expectedLogId)
    {
        var order = TrackForRow(row);
        _database.SaveFailure = () => TranslatedFailure(variant, order);

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
        result.Error.Type.Should().Be(ErrorType.Conflict, "규칙 1 ~ 3의 결과는 모두 충돌(409)");
        _logger.Collector.GetSnapshot().Should().ContainSingle().Which.Id.Id.Should().Be(expectedLogId);
        _database.Steps.Should().Equal(BeginStep, "Save", "Dispose");
        order.DomainEvents.Should().NotBeEmpty("실패 뒤에는 ClearDomainEvents를 하지 않는다");
        _context.Entry(order).State.Should().NotBe(EntityState.Unchanged, "실패 뒤에는 AcceptAllChanges를 하지 않는다");
    }

    [Fact]
    public async Task CommitAsync_ConcurrencyConflict_ReturnsCommonConcurrencyConflictResultForApiMapping()
    {
        // FR-07 "DbUpdateConcurrencyException → 충돌 Result": 공통 API 처리가 409로 바꿀 3001 Result가 나온다.
        var order = TrackForRow(1);
        _database.SaveFailure = () => SamplePostgresExceptions.Concurrency(_context.Entry(order));

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.Error.Should().Be(CommonErrors.ConcurrencyConflict);
    }

    // ---- 규칙 1 ~ 3: 개인정보(Detail · MessageText · 엔트리 값)가 Result · 로그에 없다 ----

    [Theory]
    [InlineData("concurrency")]
    [InlineData("mapped ux_")]
    [InlineData("pk_")]
    public async Task CommitAsync_TranslatedRuleRow_ResultAndLogNeverContainSecretValueOrExceptionObject(string variant)
    {
        // 엔트리 값(주문 번호)과 PostgresException Detail · MessageText에 같은 비밀 값을 넣는다.
        var order = SampleDbContexts.NewOrder(SamplePostgresExceptions.SecretValue);
        _context.Orders.Add(order);
        _database.SaveFailure = () => TranslatedFailure(variant, order);

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.Error.Message.Should().NotContain(SamplePostgresExceptions.SecretValue);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Exception.Should().BeNull();
        record.Message.Should().NotContain(SamplePostgresExceptions.SecretValue);
        record.StructuredState!.Should().NotContain(pair => (pair.Value ?? string.Empty).Contains(SamplePostgresExceptions.SecretValue, StringComparison.Ordinal));
        record.StructuredState!.Select(pair => pair.Key).Should().BeSubsetOf(["ConstraintName", "SqlState", "EntityTypes", "ErrorCode", "{OriginalFormat}"]);
    }

    // ---- 규칙 4 ~ 9: 변환하지 않음 → 같은 예외, UoW 로그 없음, 재시도 없음, 커밋 없음, 상태 유지 ----

    [Theory]
    [InlineData(4, "23514")]
    [InlineData(5, "23503")]
    [InlineData(5, "23502")]
    [InlineData(5, "22001")]
    [InlineData(5, "25006")]
    [InlineData(6, "inner null")]
    [InlineData(6, "inner NpgsqlException")]
    [InlineData(6, "inner InvalidOperationException")]
    [InlineData(6, "23505 two levels deep")]
    [InlineData(6, "DbUpdateException in DbUpdateException")]
    [InlineData(7, "unwrapped 23505 at save")]
    [InlineData(8, "retry limit")]
    [InlineData(9, "canceled")]
    public async Task CommitAsync_UntranslatedRuleRow_RethrowsSameExceptionWithoutLogRetryOrCommit(int row, string variant)
    {
        var order = TrackForRow(row);
        var exception = UntranslatedFailure(variant);
        _database.SaveFailure = () => exception;

        var act = () => _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(exception);
        _logger.Collector.Count.Should().Be(0);
        _database.Steps.Should().Equal(BeginStep, "Save", "Dispose");
        order.DomainEvents.Should().NotBeEmpty();
        _context.Entry(order).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task CommitAsync_Rule7UnwrappedUniqueViolationAtCommit_RethrowsAfterSingleAttemptAndKeepsState()
    {
        // 감싸지 않은 PostgresException(COMMIT에서 발생)은 23505여도 변환하지 않고, 일시 오류가 아니라 재시도도 하지 않는다.
        var order = TrackForRow(7);
        var exception = SamplePostgresExceptions.Create(PostgresErrorCodes.UniqueViolation, SampleIndexNames.OrdersOrderNumber.Value);
        _database.FailNextCommit(exception);

        var act = () => _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<PostgresException>()).Which.Should().BeSameAs(exception);
        _logger.Collector.Count.Should().Be(0);
        _database.Steps.Should().Equal(BeginStep, "Save", "Hook", "CommitFailed", "Dispose");
        order.DomainEvents.Should().NotBeEmpty();
    }

    // ---- 재시도: 일시 오류는 전략이 다시 실행하고, 두 번째 시도도 accept false 덕분에 같은 엔트리 상태를 본다 ----

    [Fact]
    public async Task CommitAsync_TransientFailureAtSaveThenSuccess_RetriesWholeUnitWithSameEntryStatesAndClearsEventsOnce()
    {
        // DbUpdateException 안의 40001은 실행 전략이 안쪽 예외로 일시 오류를 판단해 재시도한다(첫 재시도 지연 0).
        var order = TrackForRow(0);
        var transient = SamplePostgresExceptions.Wrap(SamplePostgresExceptions.Create(PostgresErrorCodes.SerializationFailure));
        _database.SaveFailure = () =>
        {
            _database.SaveFailure = null;
            return transient;
        };

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _database.Steps.Should().Equal(BeginStep, "Save", "Dispose", BeginStep, "Save", "Hook", "Commit", "Dispose");
        _hook.EntryStates.Should().ContainSingle().Which.Should().NotBeEmpty().And.OnlyContain(state => state == EntityState.Added);
        _logger.Collector.Count.Should().Be(0, "재시도 로그는 EF 실행 전략이 남기고 UoW는 따로 남기지 않는다");
        _context.Entry(order).State.Should().Be(EntityState.Unchanged);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_TransientCommitFailureRetried_HookSeesSameEntryStatesOnBothAttempts()
    {
        // accept false: 첫 시도의 SaveChanges가 성공한 뒤 커밋이 실패해도 엔트리는 Added 그대로라 재시도가 같은 INSERT를 다시 보낸다.
        var order = TrackForRow(0);
        _database.FailNextCommit(SamplePostgresExceptions.Create(PostgresErrorCodes.DeadlockDetected));

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _hook.EntryStates.Should().HaveCount(2);
        _hook.EntryStates[1].Should().NotBeEmpty().And.Equal(_hook.EntryStates[0]).And.OnlyContain(state => state == EntityState.Added);
        _context.Entry(order).State.Should().Be(EntityState.Unchanged);
    }

    // ---- 로그 대응: 변환 종류마다 로그가 하나씩 있다(UnitOfWork Log switch) ----

    [Fact]
    public void PersistenceFailureKind_Values_AreExactlyTheThreeLoggedKindsPlusReservedNone()
    {
        // 새 종류를 추가하면 UnitOfWork.Log switch · PersistenceLogs · error-codes.md "영속성 로그 이벤트"와 위 규칙 행 테스트를 같이 바꾼다.
        // 기존 세 종류의 로그 대응(203 · 201 · 202)은 CommitAsync_TranslatedRuleRow_ReturnsDocumentedCodeAndLogWithoutRetryOrCommit가 확인한다.
        Enum.GetValues<PersistenceFailureKind>().Should().Equal(
            PersistenceFailureKind.None,
            PersistenceFailureKind.ConcurrencyConflict,
            PersistenceFailureKind.MappedUniqueViolation,
            PersistenceFailureKind.UnmappedUniqueViolation);
    }

    private static Exception UntranslatedFailure(string variant) => variant switch
    {
        "23514" => Wrapped(PostgresErrorCodes.CheckViolation, "ck_orders_order_status"),
        "23503" => Wrapped(PostgresErrorCodes.ForeignKeyViolation, "fk_orders_customer_id"),
        "23502" => Wrapped(PostgresErrorCodes.NotNullViolation, null),
        "22001" => Wrapped(PostgresErrorCodes.StringDataRightTruncation, null),
        "25006" => Wrapped(PostgresErrorCodes.ReadOnlySqlTransaction, null),
        "inner null" => SamplePostgresExceptions.Wrap(null),
        "inner NpgsqlException" => SamplePostgresExceptions.Wrap(new NpgsqlException("protocol error")),
        "inner InvalidOperationException" => SamplePostgresExceptions.Wrap(new InvalidOperationException("not a database error")),
        "23505 two levels deep" => SamplePostgresExceptions.Wrap(
            new InvalidOperationException("wrapper", SamplePostgresExceptions.Create(PostgresErrorCodes.UniqueViolation, SampleIndexNames.OrdersOrderNumber.Value))),
        "DbUpdateException in DbUpdateException" => SamplePostgresExceptions.Wrap(
            SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value)),
        "unwrapped 23505 at save" => SamplePostgresExceptions.Create(PostgresErrorCodes.UniqueViolation, SampleIndexNames.OrdersOrderNumber.Value),
        "retry limit" => new RetryLimitExceededException("retry limit", SamplePostgresExceptions.Create(PostgresErrorCodes.SerializationFailure)),
        "canceled" => new OperationCanceledException(),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "정의되지 않은 규칙 행 변형"),
    };

    private static DbUpdateException Wrapped(string sqlState, string? constraintName) =>
        SamplePostgresExceptions.Wrap(SamplePostgresExceptions.Create(sqlState, constraintName));

    private DbUpdateException TranslatedFailure(string variant, Order order) => variant switch
    {
        "concurrency" => SamplePostgresExceptions.Concurrency(_context.Entry(order)),
        "mapped ux_" => SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value, _context.Entry(order)),
        "pk_" => SamplePostgresExceptions.UniqueViolation("pk_orders", _context.Entry(order)),
        "null" => SamplePostgresExceptions.UniqueViolation(null, _context.Entry(order)),
        "empty" => SamplePostgresExceptions.UniqueViolation(string.Empty, _context.Entry(order)),
        "unmapped ux_" => SamplePostgresExceptions.UniqueViolation("ux_orders_customer_email", _context.Entry(order)),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "정의되지 않은 규칙 행 변형"),
    };

    // 규칙 1(동시성)은 UPDATE 경로라 추적 중 Modified, 나머지는 INSERT 경로라 Added로 둔다.
    private Order TrackForRow(int row)
    {
        var order = SampleDbContexts.NewOrder();
        if (row == 1)
        {
            _context.Attach(order);
            order.Ship();
        }
        else
        {
            _context.Orders.Add(order);
        }

        return order;
    }
}
