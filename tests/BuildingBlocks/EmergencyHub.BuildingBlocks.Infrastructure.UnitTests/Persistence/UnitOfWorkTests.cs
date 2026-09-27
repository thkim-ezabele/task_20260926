using System.Data;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Npgsql;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// database.md "UnitOfWork 커밋 순서": 실행 전략 안에서 ReadCommitted 트랜잭션 → SaveChanges(accept false) → Outbox 확장 지점 → 커밋,
// 전략 밖에서 IHasDomainEvents 수집 → AcceptAllChanges → ClearDomainEvents. 예외 변환은 전략 바깥에서 한다(규칙표 1~9, 로그 201~203).
// DB 없이 FakeDatabase 인터셉터가 연결 · 트랜잭션 · 저장 · 커밋을 건너뛰고 단계를 기록한다(실행 전략은 실제 Npgsql 재시도 전략).
// accept false의 실제 의미(저장 뒤 상태 유지), 실제 롤백 · 재시도 지연 · 동시성은 S03-T05 통합 테스트가 확인한다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class UnitOfWorkTests : IDisposable
{
    private readonly FakeDatabase _database = new();
    private readonly FakeLogger<UnitOfWork<SampleWriteDbContext>> _logger = new();
    private readonly SampleWriteDbContext _context;
    private readonly RecordingPreCommitHook _hook;
    private readonly UnitOfWork<SampleWriteDbContext> _unitOfWork;

    public UnitOfWorkTests()
    {
        _context = SampleDbContexts.CreateWrite(_database);
        _hook = new RecordingPreCommitHook(_database);
        _unitOfWork = new UnitOfWork<SampleWriteDbContext>(_context, SampleUniqueConstraintErrors.Create(), [_hook], _logger);
    }

    public void Dispose() => _context.Dispose();

    // ---- 성공: 순서 ----

    [Fact]
    public async Task CommitAsync_Success_RunsReadCommittedTransactionSaveHookCommitInOrder()
    {
        _context.Orders.Add(SampleDbContexts.NewOrder());

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _database.Steps.Should().Equal($"Begin:{IsolationLevel.ReadCommitted}", "Save", "Hook", "Commit", "Dispose");
    }

    [Fact]
    public async Task CommitAsync_Success_PreCommitHookSeesSameContextBeforeAcceptAllChanges()
    {
        _context.Orders.Add(SampleDbContexts.NewOrder());

        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        _hook.Contexts.Should().ContainSingle().Which.Should().BeSameAs(_context);
        _hook.EntryStates.Should().ContainSingle().Which.Should().Contain(EntityState.Added);
    }

    [Fact]
    public async Task CommitAsync_Success_AcceptsAllChangesAndClearsDomainEvents()
    {
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);
        order.DomainEvents.Should().NotBeEmpty();

        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        _context.Entry(order).State.Should().Be(EntityState.Unchanged);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_SuccessWithDeletedAggregate_ClearsItsEventsEvenThoughEntryIsDetached()
    {
        // AcceptAllChanges가 Deleted 엔트리를 Detached로 빼므로 IHasDomainEvents 대상은 그 전에 모아야 한다.
        var order = SampleDbContexts.NewOrder();
        _context.Attach(order);
        _context.Orders.Remove(order);
        order.DomainEvents.Should().NotBeEmpty();

        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        _context.Entry(order).State.Should().Be(EntityState.Detached);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_SuccessWithSeveralAggregates_ClearsEventsOfEveryTrackedAggregate()
    {
        var first = SampleDbContexts.NewOrder("ORD-1");
        var second = SampleDbContexts.NewOrder("ORD-2");
        _context.Orders.AddRange(first, second);

        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        first.DomainEvents.Should().BeEmpty();
        second.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_NoChanges_StillSucceedsAndWritesNoLog()
    {
        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task CommitAsync_WithoutPreCommitHooks_CommitsWithoutHookStep()
    {
        var unitOfWork = new UnitOfWork<SampleWriteDbContext>(_context, SampleUniqueConstraintErrors.Create(), [], _logger);
        _context.Orders.Add(SampleDbContexts.NewOrder());

        var result = await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _database.Steps.Should().Equal($"Begin:{IsolationLevel.ReadCommitted}", "Save", "Commit", "Dispose");
    }

    // ---- 재시도: Outbox 확장 지점은 다시 호출된다 → 멱등이어야 함 ----

    [Fact]
    public async Task CommitAsync_TransientCommitFailureRetried_CallsPreCommitHookAgainSoHookMustBeIdempotent()
    {
        // 첫 재시도 지연은 0이다(EF 실행 전략 지수 백오프의 첫 항). 두 번째 시도에서 트랜잭션 · 저장 · 확장 지점이 다시 실행된다.
        _database.FailNextCommit(SamplePostgresExceptions.Create(PostgresErrorCodes.SerializationFailure));
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _hook.Contexts.Should().HaveCount(2);
        _database.Steps.Should().Equal(
            $"Begin:{IsolationLevel.ReadCommitted}", "Save", "Hook", "CommitFailed", "Dispose",
            $"Begin:{IsolationLevel.ReadCommitted}", "Save", "Hook", "Commit", "Dispose");
        order.DomainEvents.Should().BeEmpty();
    }

    // ---- 실패: 변환(규칙 1~3) → 실패 Result, 롤백 뒤, 추적 상태 유지 ----

    [Fact]
    public async Task CommitAsync_MappedUniqueViolation_ReturnsServiceErrorAfterRollbackWithoutCommit()
    {
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);
        _database.SaveFailure = () => SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value, _context.Entry(order));

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleErrors.ValueConflict);
        _database.Steps.Should().Equal($"Begin:{IsolationLevel.ReadCommitted}", "Save", "Dispose");
    }

    [Fact]
    public async Task CommitAsync_FailedResult_KeepsTrackedStateAndDomainEvents()
    {
        // 실패 뒤에는 AcceptAllChanges · ClearDomainEvents를 하지 않는다(스코프 하나 = Command 하나).
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);
        _database.SaveFailure = () => SamplePostgresExceptions.UniqueViolation("pk_orders", _context.Entry(order));

        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        _context.Entry(order).State.Should().Be(EntityState.Added);
        order.DomainEvents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CommitAsync_MappedUniqueViolation_LogsEvent201AtDebugWithConstraintSqlStateEntityTypeAndCode()
    {
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);
        _database.SaveFailure = () => SamplePostgresExceptions.UniqueViolation(SampleIndexNames.OrdersOrderNumber.Value, _context.Entry(order));

        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(201);
        record.Level.Should().Be(LogLevel.Debug);
        record.GetStructuredStateValue("ConstraintName").Should().Be(SampleIndexNames.OrdersOrderNumber.Value);
        record.GetStructuredStateValue("SqlState").Should().Be(PostgresErrorCodes.UniqueViolation);
        record.GetStructuredStateValue("EntityTypes").Should().Be(nameof(Order));
        record.GetStructuredStateValue("ErrorCode").Should().Be("23001");
    }

    [Theory]
    [InlineData("pk_orders")]
    [InlineData("ux_orders_unknown")]
    [InlineData(null)]
    public async Task CommitAsync_UnmappedUniqueViolation_Returns3003AndLogsEvent202AtWarning(string? constraintName)
    {
        _context.Orders.Add(SampleDbContexts.NewOrder());
        _database.SaveFailure = () => SamplePostgresExceptions.UniqueViolation(constraintName);

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.Error.Should().Be(CommonErrors.UniqueConstraintViolated);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(202);
        record.Level.Should().Be(LogLevel.Warning);
        record.GetStructuredStateValue("ErrorCode").Should().Be("3003");
    }

    [Fact]
    public async Task CommitAsync_ConcurrencyConflict_Returns3001AndLogsEvent203AtDebug()
    {
        var order = SampleDbContexts.NewOrder();
        _context.Attach(order);
        order.Ship();
        _database.SaveFailure = () => SamplePostgresExceptions.Concurrency(_context.Entry(order));

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.Error.Should().Be(CommonErrors.ConcurrencyConflict);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(203);
        record.Level.Should().Be(LogLevel.Debug);
        record.GetStructuredStateValue("EntityTypes").Should().Be(nameof(Order));
        record.GetStructuredStateValue("ErrorCode").Should().Be("3001");
    }

    [Fact]
    public async Task CommitAsync_UniqueViolation_LogAndResultNeverContainDetailMessageTextOrException()
    {
        var order = SampleDbContexts.NewOrder(SamplePostgresExceptions.SecretValue);
        _context.Orders.Add(order);
        _database.SaveFailure = () => SamplePostgresExceptions.UniqueViolation("pk_orders", _context.Entry(order));

        var result = await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        result.Error.Message.Should().NotContain(SamplePostgresExceptions.SecretValue);
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Exception.Should().BeNull("로그 호출에 예외 객체를 넘기지 않는다(싱크가 내부 예외를 펼침)");
        record.Message.Should().NotContain(SamplePostgresExceptions.SecretValue);
        record.StructuredState!.Select(pair => pair.Value ?? string.Empty)
            .Should().NotContain(value => value.Contains(SamplePostgresExceptions.SecretValue, StringComparison.Ordinal));
        record.StructuredState!.Select(pair => pair.Key)
            .Should().BeEquivalentTo("ConstraintName", "SqlState", "EntityTypes", "ErrorCode", "{OriginalFormat}");
    }

    // ---- 실패: 변환하지 않음(규칙 4 · 5 · 6 · 7 · 8 · 9) → 같은 예외, UoW 로그 없음 ----

    [Theory]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.ReadOnlySqlTransaction)]
    public async Task CommitAsync_NonUniqueSqlState_RethrowsSameExceptionWithoutLog(string sqlState)
    {
        var exception = SamplePostgresExceptions.Wrap(SamplePostgresExceptions.Create(sqlState, "ck_orders_order_status"));
        _context.Orders.Add(SampleDbContexts.NewOrder());
        _database.SaveFailure = () => exception;

        var act = () => _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<DbUpdateException>()).Which.Should().BeSameAs(exception);
        _database.Steps.Should().NotContain("Commit");
        _logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task CommitAsync_UnwrappedPostgresUniqueViolationAtCommit_RethrowsWithoutTranslation()
    {
        var exception = SamplePostgresExceptions.Create(PostgresErrorCodes.UniqueViolation, SampleIndexNames.OrdersOrderNumber.Value);
        _database.FailNextCommit(exception);
        _context.Orders.Add(SampleDbContexts.NewOrder());

        var act = () => _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<PostgresException>()).Which.Should().BeSameAs(exception);
        _logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task CommitAsync_RetryLimitExceeded_RethrowsForExceptionClassifier()
    {
        _context.Orders.Add(SampleDbContexts.NewOrder());
        _database.SaveFailure = () => new RetryLimitExceededException("retry limit");

        var act = () => _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RetryLimitExceededException>();
        _logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task CommitAsync_OperationCanceledDuringSave_PropagatesCancellationWithoutClearingEvents()
    {
        var order = SampleDbContexts.NewOrder();
        _context.Orders.Add(order);
        _database.SaveFailure = () => new OperationCanceledException();

        var act = () => _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
        order.DomainEvents.Should().NotBeEmpty();
        _context.Entry(order).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task CommitAsync_AlreadyCanceledToken_ThrowsOperationCanceledWithoutCommit()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _context.Orders.Add(SampleDbContexts.NewOrder());

        var act = () => _unitOfWork.CommitAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _database.Steps.Should().NotContain("Commit");
    }
}
