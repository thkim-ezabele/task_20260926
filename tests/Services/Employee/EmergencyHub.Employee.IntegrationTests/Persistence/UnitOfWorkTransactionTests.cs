using System.Data;
using System.Diagnostics;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.FaultInjection;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Persistence;

// BL-081 트랜잭션 · UoW 실측(testing-strategy.md "장애 주입", ADR-0014): 실제 PostgreSQL에서 UnitOfWork.CommitAsync의 트랜잭션 경계를 확인한다.
// P1 격리 수준(서버 관찰), P2 커밋 시점 일시 오류 1회 → 재시도 뒤 행 1개, P3 PreCommitHook 예외 → 롤백, P6 매번 일시 오류 → 재시도 한도 초과(9003 분류),
// P7 Deleted Aggregate 이벤트 비움. P8(로그)은 EfCoreErrorLogLevelTests · PersistenceLogExposureTests, P4 · P5는 UnitOfWorkConflictTests.
// 트리거 테스트는 폐기 뒤 잔여 검사(TestObjectCounts.None)를 단언한다(뒤 테스트 보호).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-05")]
public sealed class UnitOfWorkTransactionTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string EmployeeCountSql = "SELECT count(*) FROM employees";

    // 재시도 한도를 2회 · 10ms로 줄인다(운영 Api 3회 · 5초, 기본 6회 · 30초는 실패 경로에서 수 초 ~ 1분이 걸림). 시도 수 = MaxRetryCount + 1.
    private static readonly DbRetryOptions FastRetry = new(maxRetryCount: 2, maxRetryDelay: TimeSpan.FromMilliseconds(10));

    // ---- 성공 ----

    [Fact]
    public async Task CommitAsync_ServerDefaultIsolationSerializable_RunsInReadCommittedTransaction()
    {
        // P1: 서버 기본값을 serializable로 바꾼 연결에서도 UnitOfWork가 명시한 read committed로 실행된다(트리거가 서버 값을 기록, 인터셉터는 요청 값 보조 확인).
        var transactions = new TransactionProbeInterceptor();
        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            WriteConnectionString = Database.WriteConnectionStringWith(builder => builder.Options = "-c default_transaction_isolation=serializable"),
            WriteInterceptors = [transactions],
        });
        Result result;
        IReadOnlyList<IsolationObservation> observations;

        await using (var probe = await TestTriggers.CreateIsolationProbeAsync(Database, CancellationToken))
        {
            result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);
            observations = await probe.ReadIsolationObservationsAsync(CancellationToken);
        }

        result.IsSuccess.Should().BeTrue();
        observations.Should().Equal(new IsolationObservation("read committed", "off", "serializable"));
        transactions.StartedIsolationLevels.Should().Equal(IsolationLevel.ReadCommitted);
        (await TestTriggers.CountLeftoversAsync(Database, CancellationToken)).Should().Be(TestObjectCounts.None);
    }

    [Fact]
    public async Task CommitAsync_CommitTimeSerializationFailureOnce_RetriesWholeUnitAndStoresExactlyOneRow()
    {
        // P2: 서버가 COMMIT에서 40001을 내고 롤백 → 실행 전략이 트랜잭션 · SaveChanges · PreCommitHook · 커밋을 다시 실행 → 행은 정확히 1개.
        var transactions = new TransactionProbeInterceptor();
        var hook = new CountingPreCommitHook();
        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            Retry = FastRetry,
            WriteInterceptors = [transactions],
            ConfigureServices = registrations => registrations.AddSingleton<IPreCommitHook>(hook),
        });
        var employee = new EmployeeBuilder().Build();
        Result result;
        long attempts;

        await using (var trigger = await TestTriggers.CreateCommitFailureOnceAsync(Database, CancellationToken))
        {
            result = await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken);
            attempts = await trigger.ReadAttemptsAsync(CancellationToken);
        }

        result.IsSuccess.Should().BeTrue();
        attempts.Should().Be(2);
        transactions.CommitAttempts.Should().Be(2);
        transactions.Commits.Should().Be(1);
        hook.Calls.Should().Be(2, "재시도 때 PreCommitHook도 다시 호출되므로 멱등이어야 한다(IPreCommitHook 계약)");
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>(EmployeeCountSql, CancellationToken)).Should().Be(1);
        (await connection.ScalarAsync<long>("SELECT count(*) FROM employees WHERE id = $1", CancellationToken, employee.Id.Value)).Should().Be(1);
        (await TestTriggers.CountLeftoversAsync(Database, CancellationToken)).Should().Be(TestObjectCounts.None);
    }

    [Fact]
    public async Task CommitAsync_DeletedAggregateWithDomainEvents_DeletesRowAndClearsEvents()
    {
        // P7: AcceptAllChanges가 Deleted 엔트리를 Detached로 빼기 전에 Aggregate를 모으므로, 삭제한 Aggregate의 이벤트도 비워진다(실제 DELETE · xmin 검사 포함).
        await using var services = Database.CreateServices();
        var id = new EmployeeBuilder().Build().Id;
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithId(id).Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        var version = await ReadVersionAsync(id.Value);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        var deleted = new EmployeeBuilder().WithId(id).Build();
        db.Attach(deleted);
        db.Entry(deleted).Property(ShadowPropertyNames.Version).OriginalValue = version;
        db.Remove(deleted);
        deleted.DomainEvents.Should().NotBeEmpty();

        var result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken);

        result.IsSuccess.Should().BeTrue();
        deleted.DomainEvents.Should().BeEmpty();
        db.Entry(deleted).State.Should().Be(EntityState.Detached);
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>(EmployeeCountSql, CancellationToken)).Should().Be(0);
    }

    // ---- 실패 ----

    [Fact]
    public async Task CommitAsync_PreCommitHookThrows_RollsBackSavedRowAndPropagatesWithoutRetry()
    {
        // P3: SaveChanges는 트랜잭션 안에서 이미 실행됐고(Hook이 같은 트랜잭션에서 행 1개를 봄), Hook 예외로 커밋 없이 롤백된다.
        var hook = new ThrowingPreCommitHook();
        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            Retry = FastRetry,
            ConfigureServices = registrations => registrations.AddSingleton<IPreCommitHook>(hook),
        });

        var act = () => EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage(ThrowingPreCommitHook.Message);
        hook.Calls.Should().Be(1, "일시 오류가 아닌 예외는 재시도하지 않는다");
        hook.RowsSeenInTransaction.Should().Be(1);
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>(EmployeeCountSql, CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task CommitAsync_SerializationFailureOnEveryAttempt_ThrowsRetryLimitExceededClassifiedAs9003()
    {
        // P6: 매번 40001 → 실행 전략이 MaxRetryCount + 1회 시도 뒤 RetryLimitExceededException. UnitOfWork는 변환하지 않고,
        // Infrastructure 분류기(IExceptionClassifier)가 전역 예외 처리기 경로에서 9003으로 분류한다(HTTP 응답은 S03-T07).
        await using var services = Database.CreateServices(new EmployeeServicesOptions { Retry = FastRetry });
        Func<Task<Result>> act = () => EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);
        RetryLimitExceededException exception;
        long attempts;

        await using (var trigger = await TestTriggers.CreateAlwaysFailAsync(Database, CancellationToken))
        {
            exception = (await act.Should().ThrowAsync<RetryLimitExceededException>()).Which;
            attempts = await trigger.ReadAttemptsAsync(CancellationToken);
        }

        attempts.Should().Be(FastRetry.MaxRetryCount + 1);
        exception.InnerException.Should().BeOfType<DbUpdateException>()
            .Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.SerializationFailure);
        services.GetServices<IExceptionClassifier>().Select(classifier => classifier.Classify(exception)).OfType<Error>()
            .Should().ContainSingle().Which.Code.Should().Be(CommonErrors.TemporarilyUnavailable.Code);
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        (await connection.ScalarAsync<long>(EmployeeCountSql, CancellationToken)).Should().Be(0);
        (await TestTriggers.CountLeftoversAsync(Database, CancellationToken)).Should().Be(TestObjectCounts.None);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task CommitAsync_WrongPasswordWithDefaultRetry_FailsFastWith28P01WithoutRetrying()
    {
        // 외부 연동 영구 실패: 28P01은 일시 오류가 아니라 Npgsql 기본 재시도(6회 · 30초, 약 57초)를 타지 않고 곧바로 올라온다(S03-T05 실측 약 1초).
        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            WriteConnectionString = Database.WriteConnectionStringWith(builder => builder.Password = $"wrong{Guid.NewGuid():N}"),
        });
        var stopwatch = Stopwatch.StartNew();

        var act = () => EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.InvalidPassword);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }

    private async Task<uint> ReadVersionAsync(Guid id)
    {
        await using var connection = await Database.OpenWriteConnectionAsync(CancellationToken);
        return checked((uint)await connection.ScalarAsync<long>("SELECT xmin::text::bigint FROM employees WHERE id = $1", CancellationToken, id));
    }

    private sealed class CountingPreCommitHook : IPreCommitHook
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task BeforeCommitAsync(WriteDbContextBase context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPreCommitHook : IPreCommitHook
    {
        public const string Message = "test fault P3: pre-commit hook failure";

        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public long RowsSeenInTransaction { get; private set; }

        public async Task BeforeCommitAsync(WriteDbContextBase context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            RowsSeenInTransaction = await context.Set<Domain.Employees.Employee>().LongCountAsync(cancellationToken);
            throw new InvalidOperationException(Message);
        }
    }
}
