using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.IntegrationTests.FaultInjection;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.Persistence;

// BL-023 결정 고정(S03-T06): UseBuildingBlocksNpgsql 한 곳에서 EF의 CommandError(20102) · SaveChangesFailed(10000) · TransactionError(20205)를 Debug로 낮춘다.
// 정상 경합(23505 → 23001)과 재시도로 회복한 일시 오류에 Error 로그가 남지 않고, 변환되지 않는 예외는 그대로 전파된다
// (Error 로그는 전역 예외 처리기 이벤트 1이 한 번 남김, BuildingBlocks.Api 단위 테스트 GlobalExceptionHandlerTests · ExceptionResponseAcceptanceTests).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-05")]
public sealed class EfCoreErrorLogLevelTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string EfCategoryPrefix = "Microsoft.EntityFrameworkCore";

    private static readonly DbRetryOptions FastRetry = new(maxRetryCount: 2, maxRetryDelay: TimeSpan.FromMilliseconds(10));

    // ---- 성공 ----

    [Fact]
    public async Task CommitAsync_DuplicateEmail_Returns23001WithEfFailureLogsAtDebugOnly()
    {
        await using var services = Database.CreateServices();
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithEmail(email).Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        services.GetFakeLogCollector().Clear();

        var result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithEmail(email).Build(), CancellationToken);

        result.Error.Code.Should().Be(EmployeeErrors.DuplicateEmail.Code);
        var logs = EfLogs(services);
        logs.Should().NotContain(record => record.Level >= LogLevel.Error);
        logs.Should().Contain(record => record.Id.Id == RelationalEventId.CommandError.Id && record.Level == LogLevel.Debug);
        logs.Should().Contain(record => record.Id.Id == CoreEventId.SaveChangesFailed.Id && record.Level == LogLevel.Debug);
        logs.Should().NotContain(record => record.Message.Contains(email, StringComparison.OrdinalIgnoreCase));
    }

    // ---- 실패 ----

    [Fact]
    public async Task CommitAsync_CheckViolation_PropagatesDbUpdateExceptionWithoutEfErrorLogs()
    {
        await using var services = Database.CreateServices(new EmployeeServicesOptions { Retry = FastRetry });
        await using var scope = services.CreateAsyncScope();
        var employee = new EmployeeBuilder().Build();
        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Entry(employee).Property(e => e.EmployeeStatus).CurrentValue = (EmployeeStatus)9;

        var act = () => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken);

        (await act.Should().ThrowAsync<DbUpdateException>()).WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        EfLogs(services).Should().NotContain(record => record.Level >= LogLevel.Error);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task CommitAsync_CommitTimeSerializationFailureOnce_RetriesToSuccessWithoutEfErrorLogs()
    {
        await using var services = Database.CreateServices(new EmployeeServicesOptions { Retry = FastRetry });
        Result result;
        long attempts;

        await using (var trigger = await TestTriggers.CreateCommitFailureOnceAsync(Database, CancellationToken))
        {
            result = await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().Build(), CancellationToken);
            attempts = await trigger.ReadAttemptsAsync(CancellationToken);
        }

        result.IsSuccess.Should().BeTrue();
        attempts.Should().Be(2);
        var logs = EfLogs(services);
        logs.Should().NotContain(record => record.Level >= LogLevel.Error);
        logs.Should().Contain(record => record.Id.Id == RelationalEventId.TransactionError.Id && record.Level == LogLevel.Debug);
        (await TestTriggers.CountLeftoversAsync(Database, CancellationToken)).Should().Be(TestObjectCounts.None);
    }

    private static IReadOnlyList<FakeLogRecord> EfLogs(ServiceProvider services) =>
        [.. services.GetFakeLogCollector().GetSnapshot().Where(record => record.Category?.StartsWith(EfCategoryPrefix, StringComparison.Ordinal) == true)];
}
